using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Dynamit;
using Newtonsoft.Json.Linq;
using RESTar.Linq;
using RESTar.Operations;
using Starcounter;
using static System.Reflection.BindingFlags;
using static RESTar.Methods;
using static RESTar.Internal.RESTarResourceType;
using static RESTar.Operations.Transact;

namespace RESTar.Internal
{
    internal class Resource<T> : IResource<T> where T : class
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
        public bool IsSingleton { get; }
        public bool DynamicConditionsAllowed { get; }
        public string AliasOrName => Alias ?? Name;
        public override string ToString() => AliasOrName;
        public bool IsStarcounterResource { get; }
        public bool RequiresValidation { get; }
        public RESTarResourceType ResourceType { get; }

        public Selector<T> Select { get; }
        public Inserter<T> Insert { get; }
        public Updater<T> Update { get; }
        public Deleter<T> Delete { get; }
        public Counter<T> Count { get; }

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
        private Resource(string name, RESTarAttribute attribute, Selector<T> selector, Inserter<T> inserter,
            Updater<T> updater, Deleter<T> deleter, Counter<T> counter)
        {
            Name = name;
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
            ResourceType = IsStarcounterResource
                ? IsDDictionary
                    ? DynamicStarcounter
                    : StaticStarcounter
                : IsDynamic
                    ? DynamicVirtual
                    : StaticVirtual;
            Select = selector;
            Insert = inserter;
            Update = updater;
            Delete = deleter;
            Count = counter;
            CheckOperationsSupport();
            RESTarConfig.AddResource(this);
        }

        /// <summary>
        /// All custom resource registrations (using attribute as well as Resource.Register) terminate here
        /// </summary>
        internal static void Make(string name, RESTarAttribute attribute, Selector<T> selector = null,
            Inserter<T> inserter = null, Updater<T> updater = null, Deleter<T> deleter = null,
            Counter<T> counter = null)
        {
            var type = typeof(T);
            if (type.IsDDictionary() && type.Implements(typeof(IDDictionary<,>), out var _))
            {
                new Resource<T>(name, attribute,
                    type.GetSelector<T>() ?? DDictionaryOperations<T>.Select,
                    type.GetInserter<T>() ?? DDictionaryOperations<T>.Insert,
                    type.GetUpdater<T>() ?? DDictionaryOperations<T>.Update,
                    type.GetDeleter<T>() ?? DDictionaryOperations<T>.Delete,
                    type.GetCounter<T>() ?? DDictionaryOperations<T>.Count
                );
                return;
            }

            selector = selector ?? type.GetSelector<T>();
            inserter = inserter ?? type.GetInserter<T>();
            updater = updater ?? type.GetUpdater<T>();
            deleter = deleter ?? type.GetDeleter<T>();
            counter = counter ?? type.GetCounter<T>();

            if (type.HasAttribute<DatabaseAttribute>())
            {
                selector = selector ?? StarcounterOperations<T>.Select;
                inserter = inserter ?? StarcounterOperations<T>.Insert;
                updater = updater ?? StarcounterOperations<T>.Update;
                deleter = deleter ?? StarcounterOperations<T>.Delete;
                counter = counter ?? StarcounterOperations<T>.Count;
            }
            else CheckVirtualResource(type);
            new Resource<T>(name, attribute, selector, inserter, updater, deleter, counter);
        }

        private static void CheckVirtualResource(Type type)
        {
            #region Check for invalid IDictionary implementation

            if (type.Implements(typeof(IDictionary<,>), out var typeParams) && typeParams[0] != typeof(string))
                throw new VirtualResourceDeclarationException(
                    $"Invalid virtual resource declaration for type '{type.FullName}'. All resources implementing " +
                    "the generic 'System.Collections.Generic.IDictionary`2' interface must have System.String as " +
                    $"first type parameter. Found {typeParams[0].FullName}");

            #endregion

            #region Check for invalid IEnumerable implementation

            if ((type.Implements(typeof(IEnumerable<>)) || type.Implements(typeof(IEnumerable))) &&
                !type.Implements(typeof(IDictionary<,>)))
                throw new VirtualResourceDeclarationException(
                    $"Invalid virtual resource declaration for type '{type.FullName}'. The type has an invalid imple" +
                    $"mentation of an IEnumerable interface. The resource '{type.FullName}' (or any of its base types) " +
                    "cannot implement the \'System.Collections.Generic.IEnumerable`1\' or \'System.Collections.IEnume" +
                    "rable\' interfaces unless it also implements the \'System.Collections.Generic.IDictionary`2\' interface."
                );

            #endregion

            #region Check for public instance fields

            var fields = type.GetFields(Public | Instance);
            if (fields.Any())
                throw new VirtualResourceMemberException(
                    "A virtual resource cannot include public instance fields, " +
                    $"only properties. Resource: '{type.FullName}' Fields: {string.Join(", ", fields.Select(f => $"'{f.Name}'"))} in resource '{type.FullName}'");

            #endregion

            #region Check for properties with duplicate case insensitive names

            if (type.GetProperties(Public | Instance)
                .Where(p => !p.HasAttribute<IgnoreDataMemberAttribute>())
                .Where(p => !(p.DeclaringType.Implements(typeof(IDictionary<,>)) && p.Name == "Item"))
                .Select(p => p.RESTarMemberName().ToLower())
                .ContainsDuplicates(out var duplicate))
                throw new VirtualResourceMemberException(
                    $"Invalid properties for resource '{type.FullName}'. Names of public instance properties declared " +
                    $"for a virtual resource must be unique (case insensitive). Two or more property names evaluated to {duplicate}.");

            #endregion
        }


        private static IReadOnlyList<Methods> GetAvailableMethods(Type resource)
        {
            if (resource == null)
                return null;
            if (resource.HasAttribute<DynamicTableAttribute>())
                return RESTarConfig.Methods;
            return resource.GetAttribute<RESTarAttribute>()?.AvailableMethods;
        }

        private static RESTarOperations[] NecessaryOpDefs(IEnumerable<Methods> restMethods)
        {
            return restMethods.SelectMany(method =>
                {
                    switch (method)
                    {
                        case GET: return new[] {RESTarOperations.Select};
                        case POST: return new[] {RESTarOperations.Insert};
                        case PUT:
                            return new[] {RESTarOperations.Select, RESTarOperations.Insert, RESTarOperations.Update};
                        case PATCH: return new[] {RESTarOperations.Select, RESTarOperations.Update};
                        case DELETE: return new[] {RESTarOperations.Select, RESTarOperations.Delete};
                        default: return null;
                    }
                })
                .Distinct()
                .ToArray();
        }

        private void CheckOperationsSupport()
        {
            var necessaryOperations = NecessaryOpDefs(AvailableMethods);
            if (necessaryOperations.Select(op =>
                {
                    switch (op)
                    {
                        case RESTarOperations.Select: return Select;
                        case RESTarOperations.Insert: return Insert;
                        case RESTarOperations.Update: return Update;
                        case RESTarOperations.Delete: return Delete;
                        default: return default(Delegate);
                    }
                })
                .Any(result => result == null))
                throw new ArgumentException(
                    $"An operation is missing to support methods {AvailableMethods.ToMethodsString()} for " +
                    $"resource {Name}. Necessary operations: {string.Join(", ", necessaryOperations.Select(i => i.ToString()))}. " +
                    $"Make sure that the generic resource operation (e.g. ISelector<T>) interfaces have {Name} as type parameter");
        }

        public bool Equals(IResource x, IResource y) => x?.Name == y?.Name;
        public int GetHashCode(IResource obj) => obj.Name.GetHashCode();
        public int CompareTo(IResource other) => string.Compare(Name, other.Name, StringComparison.Ordinal);
        public override int GetHashCode() => Name.GetHashCode();
    }
}