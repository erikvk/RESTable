using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Resources.Operations;
using RESTable.Results;
using RESTable.Linq;

namespace RESTable.Meta.Internal
{
    internal class EntityResource<T> : IEntityResource<T>, IResourceInternal where T : class
    {
        private Dictionary<string, ITarget<T>> ViewDictionaryInternal { get; }
        public string Name { get; }
        public IReadOnlyCollection<Method> AvailableMethods { get; private set; }
        public string Description { get; private set; }
        public Type Type => typeof(T);
        public bool IsDDictionary { get; }
        public bool IsDynamic { get; }
        public bool IsInternal { get; }
        public bool IsGlobal => !IsInternal;
        public bool IsInnerResource { get; }
        public string ParentResourceName { get; }
        public bool DynamicConditionsAllowed { get; }
        public IReadOnlyDictionary<string, ITarget<T>> ViewDictionary => ViewDictionaryInternal;
        public IEnumerable<ITarget> Views => ViewDictionaryInternal?.Values;
        public TermBindingRule ConditionBindingRule { get; }
        public TermBindingRule OutputBindingRule { get; }
        public bool RequiresAuthentication => AsyncAuthenticator != null;
        public bool GETAvailableToAll { get; }
        public IReadOnlyDictionary<string, DeclaredProperty> Members { get; }
        public Type InterfaceType { get; }
        public bool DeclaredPropertiesFlagged { get; }
        public override string ToString() => Name;
        public bool RequiresValidation { get; }
        public string Provider { get; }
        public IReadOnlyList<IResource> InnerResources { get; set; }
        public bool ClaimedBy<T1>() where T1 : IEntityResourceProvider => Provider == typeof(T1).GetEntityResourceProviderId();
        public ResourceKind ResourceKind { get; }
        public bool IsDeclared { get; }
        public bool CanSelect => AsyncSelector != null;
        public bool CanInsert => AsyncInserter != null;
        public bool CanUpdate => AsyncUpdater != null;
        public bool CanDelete => AsyncDeleter != null;
        public bool CanCount => AsyncCounter != null;

        string IResourceInternal.Description
        {
            set => Description = value;
        }

        IReadOnlyCollection<Method> IResourceInternal.AvailableMethods
        {
            set => AvailableMethods = value;
        }

        public IEnumerable<T> Select(IRequest<T> request) => AsyncSelector(request);
        public int Insert(IRequest<T> request) => AsyncInserter(request);
        public int Update(IRequest<T> request) => AsyncUpdater(request);
        public int Delete(IRequest<T> request) => AsyncDeleter(request);
        public AuthResults Authenticate(IRequest<T> request) => AsyncAuthenticator(request);
        public long Count(IRequest<T> request) => AsyncCounter(request);
        
        public IEnumerable<T> Validate(IEnumerable<T> entities)
        {
            if (AsyncValidator == null) return entities;
            return entities?.Apply(e =>
            {
                if (!AsyncValidator(e, out var invalidReason))
                    throw new FailedValidation(invalidReason);
            });
        }

        private AsyncSelector<T> AsyncSelector { get; }
        private AsyncInserter<T> AsyncInserter { get; }
        private AsyncUpdater<T> AsyncUpdater { get; }
        private AsyncDeleter<T> AsyncDeleter { get; }
        private AsyncAuthenticator<T> AsyncAuthenticator { get; }
        private AsyncCounter<T> AsyncCounter { get; }
        private AsyncValidator<T> AsyncValidator { get; }
        
        /// <summary>
        /// All resources are constructed here
        /// </summary>
        internal EntityResource(string fullName, RESTableAttribute attribute, AsyncSelector<T> asyncSelector, AsyncInserter<T> asyncInserter,
            AsyncUpdater<T> asyncUpdater, AsyncDeleter<T> asyncDeleter, AsyncCounter<T> asyncCounter, AsyncAuthenticator<T> asyncAuthenticator,
            AsyncValidator<T> asyncValidator, IEntityResourceProviderInternal provider, View<T>[] views)
        {
            var typeName = typeof(T).FullName;
            if (typeName?.Contains('+') == true)
            {
                IsInnerResource = true;
                var location = typeName.LastIndexOf('+');
                ParentResourceName = typeName.Substring(0, location).Replace('+', '.');
                Name = typeName.Replace('+', '.');
            }
            else Name = fullName;
            provider.ModifyResourceAttribute(typeof(T), attribute);
            IsDeclared = attribute.IsDeclared;
            Description = attribute.Description;
            AvailableMethods = attribute.AvailableMethods;
            IsInternal = attribute is RESTableInternalAttribute;
            InterfaceType = typeof(T).GetRESTableInterfaceType();
            (DynamicConditionsAllowed, ConditionBindingRule) = typeof(T).GetDynamicConditionHandling(attribute);
            DeclaredPropertiesFlagged = attribute.FlagStaticMembers;
            GETAvailableToAll = attribute.GETAvailableToAll;
            ResourceKind = ResourceKind.EntityResource;
            if (DeclaredPropertiesFlagged)
                OutputBindingRule = TermBindingRule.DeclaredWithDynamicFallback;
            else if (typeof(T).IsDynamic() && !DeclaredPropertiesFlagged)
                OutputBindingRule = TermBindingRule.DynamicWithDeclaredFallback;
            else OutputBindingRule = TermBindingRule.OnlyDeclared;
            RequiresValidation = typeof(IAsyncValidator<>).IsAssignableFrom(typeof(T));
            IsDDictionary = false;
            IsDynamic = IsDDictionary || typeof(T).IsSubclassOf(typeof(JObject)) || typeof(IDictionary).IsAssignableFrom(typeof(T));
            Provider = provider.Id;
            Members = typeof(T).GetDeclaredProperties();

            AsyncSelector = asyncSelector.AsImplemented();
            AsyncInserter = asyncInserter.AsImplemented();
            AsyncUpdater = asyncUpdater.AsImplemented();
            AsyncDeleter = asyncDeleter.AsImplemented();
            AsyncCounter = asyncCounter.AsImplemented();
            AsyncAuthenticator = asyncAuthenticator.AsImplemented();
            AsyncValidator = asyncValidator.AsImplemented();
            
            ViewDictionaryInternal = new Dictionary<string, ITarget<T>>(StringComparer.OrdinalIgnoreCase);
            views?.ForEach(view =>
            {
                if (ViewDictionaryInternal.ContainsKey(view.Name))
                    throw new InvalidResourceViewDeclarationException(view.Type, $"Found multiple views with name '{view.Name}'.");
                ViewDictionaryInternal[view.Name] = view;
                view.SetEntityResource(this);
            });
            CheckOperationsSupport();
            RESTableConfig.AddResource(this);
        }

        private static IReadOnlyList<Method> GetAvailableMethods(Type resource)
        {
            if (resource == null)
                return null;
            return resource.GetCustomAttribute<RESTableAttribute>()?.AvailableMethods;
        }

        private static RESTableOperations[] NecessaryOpDefs(IEnumerable<Method> restMethods) => restMethods
            .SelectMany(method =>
            {
                switch (method)
                {
                    case Method.HEAD:
                    case Method.REPORT:
                    case Method.GET: return new[] {RESTableOperations.Select};
                    case Method.POST: return new[] {RESTableOperations.Insert};
                    case Method.PUT: return new[] {RESTableOperations.Select, RESTableOperations.Insert, RESTableOperations.Update};
                    case Method.PATCH: return new[] {RESTableOperations.Select, RESTableOperations.Update};
                    case Method.DELETE: return new[] {RESTableOperations.Select, RESTableOperations.Delete};
                    default: return null;
                }
            }).Distinct().ToArray();

        private Delegate GetOpDelegate(RESTableOperations op)
        {
            switch (op)
            {
                case RESTableOperations.Select: return AsyncSelector;
                case RESTableOperations.Insert: return AsyncInserter;
                case RESTableOperations.Update: return AsyncUpdater;
                case RESTableOperations.Delete: return AsyncDeleter;
                default: throw new ArgumentOutOfRangeException(nameof(op));
            }
        }

        private void CheckOperationsSupport()
        {
            foreach (var op in NecessaryOpDefs(AvailableMethods))
            {
                var del = GetOpDelegate(op);
                if (del == null)
                {
                    var @interface = DelegateMaker.MatchingInterface(op);
                    throw new InvalidResourceDeclarationException(
                        $"The '{op}' operation is needed to support method(s) {AvailableMethods.ToMethodsString()} for resource '{Name}', but " +
                        "RESTable found no implementation of the operation interface in the type declaration. Add an implementation of the " +
                        $"'{@interface.ToString().Replace("`1[T]", $"<{Name}>")}' interface to the resource's type declaration");
                }
            }
        }

        public override bool Equals(object obj) => obj is EntityResource<T> resource && resource.Name == Name;
        public bool Equals(IResource x, IResource y) => x?.Name == y?.Name;
        public int GetHashCode(IResource obj) => obj.Name.GetHashCode();
        public int CompareTo(IResource other) => string.Compare(Name, other.Name, StringComparison.Ordinal);
        public override int GetHashCode() => Name.GetHashCode();
    }
}