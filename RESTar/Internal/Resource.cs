using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using RESTar.Admin;
using RESTar.Operations;
using RESTar.Resources;
using Starcounter;
using static RESTar.Methods;
using static RESTar.Operations.DelegateMaker;
using static RESTar.Operations.Transact;
using Profiler = RESTar.Operations.Profiler;

namespace RESTar.Internal
{
    internal class Resource<T> : IResource<T>, IResourceInternal where T : class
    {
        public string Name { get; }
        public bool Editable { get; }

        // ReSharper disable MemberCanBePrivate.Global
        // ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

        public IReadOnlyList<Methods> AvailableMethods { get; internal set; }
        public string Description { get; internal set; }

        // ReSharper restore AutoPropertyCanBeMadeGetOnly.Global
        // ReSharper restore MemberCanBePrivate.Global

        public Type Type => typeof(T);
        public bool IsDDictionary { get; }
        public bool IsDynamic { get; }
        public bool IsInternal { get; }
        public bool IsGlobal => !IsInternal;
        public bool IsInnerResource { get; }
        public string ParentResourceName { get; }
        public bool IsSingleton { get; }
        public bool DynamicConditionsAllowed { get; }
        public string AliasOrName => Alias ?? Name;
        public override string ToString() => AliasOrName;
        public bool IsStarcounterResource { get; }
        public bool RequiresValidation { get; }
        public string Provider { get; }
        public IReadOnlyList<IResource> InnerResources { get; set; }
        public ResourceProfile ResourceProfile => Profile?.Invoke();
        public bool ClaimedBy<T1>() where T1 : ResourceProvider => Provider == Provider<T1>.Get;

        string IResourceInternal.Description
        {
            get => Description;
            set => Description = value;
        }

        IReadOnlyList<Methods> IResourceInternal.AvailableMethods
        {
            get => AvailableMethods;
            set => AvailableMethods = value;
        }

        public Selector<T> Select { get; }
        public Inserter<T> Insert { get; }
        public Updater<T> Update { get; }
        public Deleter<T> Delete { get; }
        public Counter<T> Count { get; }
        public Profiler Profile { get; }

        public string Alias
        {
            get => Admin.ResourceAlias.ByResource(Name)?.Alias;
            set
            {
                var existingAssignment = Admin.ResourceAlias.ByResource(Name);
                if (value == null)
                {
                    Trans(() => existingAssignment?.Delete());
                    return;
                }
                if (value == "" || value.Any(char.IsWhiteSpace))
                    throw new Exception($"Invalid alias string '{value}'. Cannot be empty or contain whitespace");
                var usedAliasMapping = Admin.ResourceAlias.ByAlias(value);
                if (usedAliasMapping != null)
                {
                    if (usedAliasMapping.Resource == Name)
                        return;
                    throw new AliasAlreadyInUseException(usedAliasMapping);
                }
                if (RESTarConfig.Resources.Any(r => r.Name.EqualsNoCase(value)))
                    throw new AliasEqualToResourceNameException(value);
                Trans(() =>
                {
                    existingAssignment = existingAssignment ?? new Admin.ResourceAlias {Resource = Name};
                    existingAssignment.Alias = value;
                });
            }
        }

        /// <summary>
        /// All custom resources are constructed here
        /// </summary>
        internal Resource(string name, RESTarAttribute attribute, Selector<T> selector, Inserter<T> inserter,
            Updater<T> updater, Deleter<T> deleter, Counter<T> counter, Profiler profiler, ResourceProvider provider)
        {
            if (name.Contains('+'))
            {
                IsInnerResource = true;
                var location = name.LastIndexOf('+');
                ParentResourceName = name.Substring(0, location).Replace('+', '.');
                Name = name.Replace('+', '.');
            }
            else Name = name;
            Editable = attribute.Editable;
            Description = attribute.Description;
            AvailableMethods = attribute.AvailableMethods;
            IsSingleton = attribute.Singleton;
            IsInternal = attribute is RESTarInternalAttribute;
            DynamicConditionsAllowed = attribute.AllowDynamicConditions;
            RequiresValidation = typeof(IValidatable).IsAssignableFrom(typeof(T));
            IsStarcounterResource = typeof(T).HasAttribute<DatabaseAttribute>();
            IsDDictionary = typeof(T).IsDDictionary();
            IsDynamic = IsDDictionary || typeof(T).IsSubclassOf(typeof(JObject)) ||
                        typeof(IDictionary).IsAssignableFrom(typeof(T));
            Provider = provider.GetProviderId();
            Select = selector;
            Insert = inserter;
            Update = updater;
            Delete = deleter;
            Count = counter;
            Profile = profiler;
            CheckOperationsSupport();
            RESTarConfig.AddResource(this);
        }

        private static IReadOnlyList<Methods> GetAvailableMethods(Type resource)
        {
            if (resource == null)
                return null;
            if (resource.HasAttribute<DynamicTableAttribute>())
                return RESTarConfig.Methods;
            return resource.GetAttribute<RESTarAttribute>()?.AvailableMethods;
        }

        private static RESTarOperations[] NecessaryOpDefs(IEnumerable<Methods> restMethods) => restMethods
            .SelectMany(method =>
            {
                switch (method)
                {
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
                    throw new ResourceDeclarationException(
                        $"The '{op}' operation is needed to support method(s) {AvailableMethods.ToMethodsString()} for resource '{Name}', but " +
                        "RESTar found no implementation of the operation interface in the type declaration. Add an implementation of the " +
                        $"'{@interface.ToString().Replace("`1[T]", $"<{Name}>")}' interface to the resource's type declaration");
                }
            }
        }

        public bool Equals(IResource x, IResource y) => x?.Name == y?.Name;
        public int GetHashCode(IResource obj) => obj.Name.GetHashCode();
        public int CompareTo(IResource other) => string.Compare(Name, other.Name, StringComparison.Ordinal);
        public override int GetHashCode() => Name.GetHashCode();
    }
}