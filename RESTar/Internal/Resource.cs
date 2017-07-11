using System;
using System.Collections;
using System.Linq;
using System.Runtime.Serialization;
using Dynamit;
using Newtonsoft.Json.Linq;
using RESTar.Operations;
using Starcounter;
using static System.Reflection.BindingFlags;
using static RESTar.Operations.Do;
using static RESTar.RESTarMethods;
using static RESTar.Internal.RESTarResourceType;
using static RESTar.Internal.Transactions;

namespace RESTar.Internal
{
    internal class Resource<T> : IResource<T> where T : class
    {
        public string Name { get; }
        public bool Editable { get; }
        public RESTarMethods[] AvailableMethods { get; }
        public string AvailableMethodsString => AvailableMethods.ToMethodsString();
        public Type TargetType { get; }
        public bool IsDDictionary { get; }
        public bool IsDynamic { get; }
        public long? NrOfEntities => Try(() => DB.RowCount(Name), default(long?));
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

        public string Alias
        {
            get => DB.Get<ResourceAlias>("Resource", Name)?.Alias;
            set
            {
                var existingMapping = DB.Get<ResourceAlias>("Resource", Name);
                if (value == null)
                {
                    Trans(() => existingMapping?.Delete());
                    return;
                }
                var usedAliasMapping = DB.Get<ResourceAlias>("Alias", value);
                if (usedAliasMapping != null)
                {
                    if (usedAliasMapping.Resource == Name)
                        return;
                    throw new Exception($"Invalid alias: '{value}' is used to refer to another resource");
                }

                Trans(() =>
                {
                    existingMapping = existingMapping ?? new ResourceAlias {Resource = Name};
                    existingMapping.Alias = value;
                });
            }
        }

        private Resource(Type targetType, bool editable, RESTarAttribute attribute, Selector<T> selector,
            Inserter<T> inserter, Updater<T> updater, Deleter<T> deleter)
        {
            Name = targetType.FullName;
            Editable = editable;
            AvailableMethods = attribute.AvailableMethods;
            IsSingleton = attribute.Singleton;
            DynamicConditionsAllowed = attribute.AllowDynamicConditions;
            RequiresValidation = typeof(IValidatable).IsAssignableFrom(targetType);
            TargetType = targetType;
            IsStarcounterResource = TargetType.HasAttribute<DatabaseAttribute>();
            IsDDictionary = typeof(T) == typeof(DDictionary);
            IsDynamic = IsDDictionary || TargetType.IsSubclassOf(typeof(JObject)) ||
                        typeof(IDictionary).IsAssignableFrom(TargetType);
            ResourceType = IsStarcounterResource
                ? IsDDictionary
                    ? ScDynamic
                    : ScStatic
                : Virtual;
            Select = selector;
            Insert = inserter;
            Update = updater;
            Delete = deleter;
            CheckOperationsSupport();
            RESTarConfig.AddResource(this);
        }

        internal static void Make(RESTarAttribute attribute, Selector<T> selector = null, Inserter<T> inserter = null,
            Updater<T> updater = null, Deleter<T> deleter = null, bool editable = false)
        {
            var type = typeof(T);
            if (type.IsSubclassOf(typeof(DDictionary)) && type.HasAttribute<DDictionaryAttribute>())
            {
                new Resource<DDictionary>(type, editable, attribute,
                    type.GetSelector<DDictionary>() ?? DDictionaryOperations.Select,
                    type.GetInserter<DDictionary>() ?? DDictionaryOperations.Insert,
                    type.GetUpdater<DDictionary>() ?? DDictionaryOperations.Update,
                    type.GetDeleter<DDictionary>() ?? DDictionaryOperations.Delete
                );
                return;
            }

            selector = selector ?? type.GetSelector<T>();
            inserter = inserter ?? type.GetInserter<T>();
            updater = updater ?? type.GetUpdater<T>();
            deleter = deleter ?? type.GetDeleter<T>();

            if (type.HasAttribute<DatabaseAttribute>())
            {
                selector = selector ?? StarcounterOperations<T>.Select;
                inserter = inserter ?? StarcounterOperations<T>.Insert;
                updater = updater ?? StarcounterOperations<T>.Update;
                deleter = deleter ?? StarcounterOperations<T>.Delete;
            }
            else
            {
                var idictionaryImplementation = type
                    .GetInterfaces()
                    .FirstOrDefault(i => i.FullName.StartsWith("System.Collections.Generic.IDictionary"));
                if (idictionaryImplementation != null)
                {
                    var firstTypeParameter = idictionaryImplementation.GenericTypeArguments.First();
                    if (firstTypeParameter != typeof(string))
                        throw new VirtualResourceDeclarationException(
                            $"Invalid virtual resource declaration for type '{type.FullName}. All resources implementing " +
                            "the generic 'System.Collections.Generic.IDictionary`2' interface must have System.String as " +
                            $"first type parameter. Found {firstTypeParameter.FullName}");
                }
                var fields = type.GetFields(Public | Instance);
                if (fields.Any())
                    throw new VirtualResourceMemberException(
                        "A virtual resource cannot include public instance fields, " +
                        $"only properties. Resource: '{type.FullName}' Fields: {string.Join(", ", fields.Select(f => $"'{f.Name}'"))} in resource '{type.FullName}'");
                var props = type.GetProperties(Public | Instance)
                    .Where(p => !p.HasAttribute<IgnoreDataMemberAttribute>())
                    .Where(p => !(p.DeclaringType?.GetInterface("IDictionary`2") != null && p.Name == "Item"))
                    .Select(p => p.RESTarMemberName().ToLower());
                if (props.ContainsDuplicates(out string duplicate))
                    throw new VirtualResourceMemberException(
                        $"Invalid properties for resource '{type.FullName}'. Names of public instance properties declared " +
                        $"for a virtual resource must be unique (case insensitive). Two or more property names evaluated to {duplicate}.");
            }

            new Resource<T>(type, editable, attribute, selector, inserter, updater, deleter);
        }

        private static RESTarMethods[] GetAvailableMethods(Type resource)
        {
            if (resource == null)
                return null;
            if (resource.HasAttribute<DynamicTableAttribute>())
                return RESTarConfig.Methods;
            return resource.GetAttribute<RESTarAttribute>()?.AvailableMethods;
        }

        private static RESTarOperations[] NecessaryOpDefs(RESTarMethods[] restMethods)
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
                    $"resource {Name}. Necessary operations: {string.Join(", ", necessaryOperations.Select(i => i.ToString()))}");
        }

        public bool Equals(IResource x, IResource y) => x.Name == y.Name;
        public int GetHashCode(IResource obj) => obj.Name.GetHashCode();
        public int CompareTo(IResource other) => string.Compare(Name, other.Name, StringComparison.Ordinal);
        public override int GetHashCode() => Name.GetHashCode();
    }
}