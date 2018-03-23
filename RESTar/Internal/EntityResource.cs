using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using RESTar.Admin;
using RESTar.Deflection;
using RESTar.Deflection.Dynamic;
using RESTar.Linq;
using RESTar.Operations;
using RESTar.Resources;
using RESTar.Results.Error;
using RESTar.Results.Error.BadRequest;
using Starcounter;
using static System.StringComparer;
using static RESTar.Method;
using static RESTar.Deflection.TermBindingRules;
using static RESTar.Operations.DelegateMaker;
using static RESTar.Operations.Transact;

namespace RESTar.Internal
{
    internal class EntityResource<T> : IEntityResource<T>, IResourceInternal where T : class
    {
        public string Name { get; }
        public bool Editable { get; }
        public IReadOnlyList<Method> AvailableMethods { get; internal set; }
        public string Description { get; internal set; }
        public Type Type => typeof(T);
        public bool IsDDictionary { get; }
        public bool IsDynamic { get; }
        public bool IsInternal { get; }
        public bool IsGlobal => !IsInternal;
        public bool IsInnerResource { get; }
        public string ParentResourceName { get; }
        public bool IsSingleton { get; }
        public bool DynamicConditionsAllowed { get; }
        private Dictionary<string, View<T>> ViewDictionaryInternal { get; }
        public IReadOnlyDictionary<string, View<T>> ViewDictionary => ViewDictionaryInternal;
        public IEnumerable<IView> Views => ViewDictionaryInternal?.Values;
        public TermBindingRules ConditionBindingRule { get; }
        public TermBindingRules OutputBindingRule { get; }
        public bool RequiresAuthentication => Authenticate != null;
        public bool GETAvailableToAll { get; }
        public IReadOnlyDictionary<string, DeclaredProperty> Members { get; }
        public void SetAlias(string alias) => Alias = alias;
        public Type InterfaceType { get; }

        /// <inheritdoc />
        /// <summary>
        /// True for DDictionary resources and dynamic resources with DynamicConditionsAllowed
        /// </summary>
        public bool DeclaredPropertiesFlagged { get; }

        public override string ToString() => Name;
        public bool IsStarcounterResource { get; }
        public bool RequiresValidation { get; }
        public string Provider { get; }
        public IReadOnlyList<IResource> InnerResources { get; set; }
        public ResourceProfile ResourceProfile => Profile?.Invoke(this);
        public bool ClaimedBy<T1>() where T1 : ResourceProvider => Provider == typeof(T1).GetProviderId();

        string IResourceInternal.Description
        {
            set => Description = value;
        }

        IReadOnlyList<Method> IResourceInternal.AvailableMethods
        {
            set => AvailableMethods = value;
        }

        public Selector<T> Select { get; }
        public Inserter<T> Insert { get; }
        public Updater<T> Update { get; }
        public Deleter<T> Delete { get; }
        public Counter<T> Count { get; }
        public Profiler<T> Profile { get; }
        public Authenticator<T> Authenticate { get; }

        public string Alias
        {
            get => Admin.ResourceAlias.GetByResource(Name)?.Alias;
            private set
            {
                var existingAssignment = Admin.ResourceAlias.GetByResource(Name);
                if (value == null)
                {
                    Trans(() => existingAssignment?.Delete());
                    return;
                }
                if (value == "" || value.Any(char.IsWhiteSpace))
                    throw new Exception($"Invalid alias string '{value}'. Cannot be empty or contain whitespace");
                var usedAliasMapping = Admin.ResourceAlias.GetByAlias(value);
                if (usedAliasMapping != null)
                {
                    if (usedAliasMapping.Resource == Name)
                        return;
                    throw new AliasAlreadyInUse(usedAliasMapping);
                }
                if (RESTarConfig.Resources.Any(r => r.Name.EqualsNoCase(value)))
                    throw new AliasEqualToResourceName(value);
                Trans(() =>
                {
                    existingAssignment = existingAssignment ?? new Admin.ResourceAlias {Resource = Name};
                    existingAssignment.Alias = value;
                });
            }
        }

        /// <summary>
        /// All resources are constructed here
        /// </summary>
        internal EntityResource(string fullName, RESTarAttribute attribute, Selector<T> selector, Inserter<T> inserter,
            Updater<T> updater, Deleter<T> deleter, Counter<T> counter, Profiler<T> profiler, Authenticator<T> authenticator,
            ResourceProvider provider, View<T>[] views)
        {
            if (fullName.Contains('+'))
            {
                IsInnerResource = true;
                var location = fullName.LastIndexOf('+');
                ParentResourceName = fullName.Substring(0, location).Replace('+', '.');
                Name = fullName.Replace('+', '.');
            }
            else Name = fullName;
            Editable = attribute.Editable;
            Description = attribute.Description;
            AvailableMethods = attribute.AvailableMethods;
            IsSingleton = attribute.Singleton;
            IsInternal = attribute is RESTarInternalAttribute;
            InterfaceType = attribute.Interface;
            DynamicConditionsAllowed = typeof(T).IsDDictionary() || attribute.AllowDynamicConditions;
            DeclaredPropertiesFlagged = typeof(T).IsDDictionary() || attribute.FlagStaticMembers;
            GETAvailableToAll = attribute.GETAvailableToAll;
            ConditionBindingRule = DynamicConditionsAllowed ? DeclaredWithDynamicFallback : OnlyDeclared;
            if (DeclaredPropertiesFlagged)
                OutputBindingRule = DeclaredWithDynamicFallback;
            else if (typeof(T).IsDynamic() && !DeclaredPropertiesFlagged)
                OutputBindingRule = DynamicWithDeclaredFallback;
            else
                OutputBindingRule = OnlyDeclared;
            RequiresValidation = typeof(IValidatable).IsAssignableFrom(typeof(T));
            IsStarcounterResource = typeof(T).HasAttribute<DatabaseAttribute>();
            IsDDictionary = typeof(T).IsDDictionary();
            IsDynamic = IsDDictionary || typeof(T).IsSubclassOf(typeof(JObject)) || typeof(IDictionary).IsAssignableFrom(typeof(T));
            Provider = provider.GetProviderId();
            Members = typeof(T).GetDeclaredProperties();
            Select = selector;
            Insert = inserter;
            Update = updater;
            Delete = deleter;
            Count = counter;
            Profile = profiler;
            Authenticate = authenticator;
            if (views?.Any() == true)
            {
                ViewDictionaryInternal = views.ToDictionary(v => v.Name, OrdinalIgnoreCase);
                views.ForEach(view => view.EntityResource = this);
            }
            CheckOperationsSupport();
            RESTarConfig.AddResource(this);
        }

        private static IReadOnlyList<Method> GetAvailableMethods(Type resource)
        {
            if (resource == null)
                return null;
            if (resource.HasAttribute<DynamicTableAttribute>())
                return RESTarConfig.Methods;
            return resource.GetAttribute<RESTarAttribute>()?.AvailableMethods;
        }

        private static RESTarOperations[] NecessaryOpDefs(IEnumerable<Method> restMethods) => restMethods
            .SelectMany(method =>
            {
                switch (method)
                {
                    case HEAD:
                    case REPORT:
                    case GET: return new[] {RESTarOperations.Select};
                    case POST: return new[] {RESTarOperations.Insert};
                    case PUT: return new[] {RESTarOperations.Select, RESTarOperations.Insert, RESTarOperations.Update};
                    case PATCH: return new[] {RESTarOperations.Select, RESTarOperations.Update};
                    case DELETE: return new[] {RESTarOperations.Select, RESTarOperations.Delete};
                    default: return null;
                }
            }).Distinct().ToArray();

        private Delegate GetOpDelegate(RESTarOperations op)
        {
            switch (op)
            {
                case RESTarOperations.Select: return Select;
                case RESTarOperations.Insert: return Insert;
                case RESTarOperations.Update: return Update;
                case RESTarOperations.Delete: return Delete;
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
                    var @interface = MatchingInterface(op);
                    throw new InvalidResourceDeclaration(
                        $"The '{op}' operation is needed to support method(s) {AvailableMethods.ToMethodsString()} for resource '{Name}', but " +
                        "RESTar found no implementation of the operation interface in the type declaration. Add an implementation of the " +
                        $"'{@interface.ToString().Replace("`1[T]", $"<{Name}>")}' interface to the resource's type declaration");
                }
            }
        }

        public override bool Equals(object obj) => obj is EntityResource<T> resource && resource.Name == Name;
        public bool Equals(IEntityResource x, IEntityResource y) => x?.Name == y?.Name;
        public int GetHashCode(IEntityResource obj) => obj.Name.GetHashCode();
        public int CompareTo(IEntityResource other) => string.Compare(Name, other.Name, StringComparison.Ordinal);
        public override int GetHashCode() => Name.GetHashCode();
    }
}