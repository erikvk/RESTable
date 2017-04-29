using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dynamit;
using RESTar.Operations;
using Starcounter;
using static System.Reflection.BindingFlags;
using static RESTar.Operations.Do;

namespace RESTar.Internal
{
    internal class Resource<T1> : IResource where T1 : class
    {
        public string Name { get; }
        public bool Editable { get; }
        public RESTarMethods[] AvailableMethods { get; }
        public string AvailableMethodsString => AvailableMethods.ToMethodsString();
        public Type TargetType { get; }
        public bool IsDynamic => typeof(T1) == typeof(DDictionary);
        public long? NrOfEntities => Try(() => DB.RowCount(Name), null);

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

        private Resource(Type targetType, bool editable, RESTarMethods[] availableMethods, Selector<T1> selector,
            Inserter<T1> inserter, Updater<T1> updater, Deleter<T1> deleter)
        {
            Name = targetType.FullName;
            Editable = editable;
            AvailableMethods = availableMethods;
            TargetType = targetType;
            Select = selector;
            Insert = (e, r) => inserter((IEnumerable<T1>) e, r);
            Update = (e, r) => updater((IEnumerable<T1>) e, r);
            Delete = (e, r) => deleter((IEnumerable<T1>) e, r);
            CheckOperationsSupport();
            RESTarConfig.AddResource(this);
        }

        internal static void Make<T>(RESTarMethods[] methods, Selector<T> selector = null, Inserter<T> inserter = null,
            Updater<T> updater = null, Deleter<T> deleter = null, bool editable = false) where T : class
        {
            var type = typeof(T);
            if (type.HasAttribute<DDictionaryAttribute>())
            {
                var dSelector = selector as Selector<DDictionary>;
                var dInserter = selector as Inserter<DDictionary>;
                var dUpdater = selector as Updater<DDictionary>;
                var dDeleter = selector as Deleter<DDictionary>;

                new Resource<DDictionary>(type, editable, methods,
                    dSelector ?? type.GetSelector<DDictionary>() ?? DynamitOperations.Select,
                    dInserter ?? type.GetInserter<DDictionary>() ?? DynamitOperations.Insert,
                    dUpdater ?? type.GetUpdater<DDictionary>() ?? DynamitOperations.Update,
                    dDeleter ?? type.GetDeleter<DDictionary>() ?? DynamitOperations.Delete
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

            new Resource<T>(type, editable, methods, selector, inserter, updater, deleter);
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
                        case RESTarMethods.GET: return new[] {RESTarOperations.Select};
                        case RESTarMethods.POST: return new[] {RESTarOperations.Insert};
                        case RESTarMethods.PUT: return new[] {RESTarOperations.Select, RESTarOperations.Insert, RESTarOperations.Update};
                        case RESTarMethods.PATCH: return new[] {RESTarOperations.Select, RESTarOperations.Update};
                        case RESTarMethods.DELETE: return new[] {RESTarOperations.Select, RESTarOperations.Delete};
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