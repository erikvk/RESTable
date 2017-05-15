using System;
using System.Collections.Generic;
using System.Linq;
using Dynamit;
using RESTar.Operations;
using Starcounter;
using static System.Reflection.BindingFlags;
using static RESTar.Operations.Do;
using static RESTar.RESTarMethods;
using static RESTar.RESTarResourceType;

namespace RESTar.Internal
{
    internal class Resource<T> : IResource where T : class
    {
        public string Name { get; }
        public bool Editable { get; }
        public RESTarMethods[] AvailableMethods { get; }
        public string AvailableMethodsString => AvailableMethods.ToMethodsString();
        public Type TargetType { get; }
        public bool IsDynamic => typeof(T) == typeof(DDictionary);
        public long? NrOfEntities => Try(() => DB.RowCount(Name), null);
        public bool Visible { get; }
        public string EntityViewHtml { get; }
        public string EntitiesViewHtml { get; }
        
        public RESTarResourceType ResourceType =>
            TargetType.HasAttribute<DatabaseAttribute>()
                ? IsDynamic
                    ? ScDynamic
                    : ScStatic
                : Virtual;

        public Selector<dynamic> Select { get; }
        public Inserter<dynamic> Insert { get; }
        public Updater<dynamic> Update { get; }
        public Deleter<dynamic> Delete { get; }

        public string Alias
        {
            get { return DB.Get<ResourceAlias>("Resource", Name)?.Alias; }
            set
            {
                var existingMapping = DB.Get<ResourceAlias>("Resource", Name);
                if (value == null)
                {
                    Db.TransactAsync(() => existingMapping?.Delete());
                    return;
                }
                var usedAliasMapping = DB.Get<ResourceAlias>("Alias", value);
                if (usedAliasMapping != null)
                {
                    if (usedAliasMapping.Resource == Name)
                        return;
                    throw new Exception($"Invalid alias: '{value}' is used to refer to another resource");
                }

                Db.TransactAsync(() =>
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
            Visible = attribute.Visible;
            TargetType = targetType;
            Select = selector;
            Insert = (e, r) => inserter((IEnumerable<T>) e, r);
            Update = (e, r) => updater((IEnumerable<T>) e, r);
            Delete = (e, r) => deleter((IEnumerable<T>) e, r);
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
                var fields = type.GetFields(Public | Instance);
                if (fields.Any())
                    throw new VirtualResourceMemberException(
                        "A virtual resource cannot include public instance fields, " +
                        $"only properties. Fields: {string.Join(", ", fields.Select(f => $"'{f.Name}'"))} in resource '{type.FullName}'"
                    );
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