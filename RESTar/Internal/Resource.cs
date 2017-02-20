using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Dynamit;
using Starcounter;

namespace RESTar.Internal
{
    internal class Resource<T1> : IResource where T1 : class
    {
        public string Name { get; }
        public bool Editable { get; }
        public ICollection<RESTarMethods> AvailableMethods { get; }
        public string AvailableMethodsString => AvailableMethods.ToMethodsString();
        public Type TargetType { get; }

        public string Alias
        {
            get { return DB.Get<ResourceAlias>("Resource", Name)?.Alias; }
            set
            {
                var existingMapping = DB.Get<ResourceAlias>("Resource", Name);
                if (value == null)
                {
                    Db.Transact(() => { existingMapping?.Delete(); });
                    return;
                }
                var usedAliasMapping = DB.Get<ResourceAlias>("Alias", value);
                if (usedAliasMapping != null)
                {
                    if (usedAliasMapping.Resource == Name)
                        return;
                    throw new Exception($"Invalid alias: '{value}' is used to refer to another resource");
                }

                Db.Transact(() =>
                {
                    existingMapping = existingMapping ?? new ResourceAlias {Resource = Name};
                    existingMapping.Alias = value;
                });
            }
        }

        public long? NrOfEntities
        {
            get
            {
                try
                {
                    return DB.RowCount(Name);
                }
                catch
                {
                    return null;
                }
            }
        }

        internal OperationsProvider<T1> Operations { get; set; }

        public IEnumerable<dynamic> Select(IRequest request) => Operations.Selector.Select(request);

        public int Insert(IEnumerable<dynamic> entities, IRequest request)
            => Operations.Inserter.Insert((IEnumerable<T1>) entities, request);

        public int Update(IEnumerable<dynamic> entities, IRequest request)
            => Operations.Updater.Update((IEnumerable<T1>) entities, request);

        public int Delete(IEnumerable<dynamic> entities, IRequest request)
            => Operations.Deleter.Delete((IEnumerable<T1>) entities, request);

        private Resource
        (
            Type targetType,
            bool editable,
            ICollection<RESTarMethods> availableMethods,
            OperationsProvider<T1> operations
        )
        {
            Name = targetType.FullName;
            Editable = editable;
            AvailableMethods = availableMethods;
            TargetType = targetType;
            Operations = operations;
            var necessaryOperations = NecessaryOpDefs(availableMethods);
            if (!operations.Supports(necessaryOperations))
                throw new ArgumentException(
                    $"An operation is missing to support methods {availableMethods.ToMethodsString()} " +
                    $"for resource {Name}. Necessary operations: " +
                    $"{string.Join(", ", necessaryOperations.Select(i => i.ToString()))}"
                );
            RESTarConfig.AddResource(this);
        }

        internal static void Make<T>
        (
            ICollection<RESTarMethods> availableMethods,
            OperationsProvider<T> operations = null,
            bool editable = false
        ) where T : class
        {
            var type = typeof(T);

            var operationsProvider = new OperationsProvider<T>
            {
                Selector = operations?.Selector ?? type.GetSelector<T>(),
                Inserter = operations?.Inserter ?? type.GetInserter<T>(),
                Updater = operations?.Updater ?? type.GetUpdater<T>(),
                Deleter = operations?.Deleter ?? type.GetDeleter<T>()
            };

            if (type.HasAttribute<DatabaseAttribute>())
            {
                if (operationsProvider.Selector == null)
                    operationsProvider.Selector = StarcounterOperations.Selector<T>();
                if (operationsProvider.Inserter == null)
                    operationsProvider.Inserter = StarcounterOperations.Inserter<T>();
                if (operationsProvider.Updater == null)
                    operationsProvider.Updater = StarcounterOperations.Updater<T>();
                if (operationsProvider.Deleter == null)
                    operationsProvider.Deleter = StarcounterOperations.Deleter<T>();
            }
            else
            {
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
                if (fields.Any())
                    throw new VirtualResourceMemberException(
                        "A virtual resource cannot include public instance fields, " +
                        $"only properties. Fields: {string.Join(", ", fields.Select(f => $"'{f.Name}'"))} in resource '{type.FullName}'"
                    );
            }


            new Resource<T>
            (
                targetType: type,
                availableMethods: availableMethods,
                editable: editable,
                operations: operationsProvider
            );
        }

        public static void Register<T>(OperationsProvider<T> operationsProvider, RESTarPresets preset,
            params RESTarMethods[] additionalMethods) where T : class
        {
            var methods = preset.ToMethods().Union(additionalMethods).ToArray();
            Register(operationsProvider, methods.First(), methods.Skip(1).ToArray());
        }

        public static void Register<T>
        (
            OperationsProvider<T> operationsProvider,
            RESTarMethods method,
            params RESTarMethods[] addMethods) where T : class
        {
            if (typeof(T).HasAttribute<RESTarAttribute>())
                throw new InvalidOperationException("Cannot manually register resources that have a RESTar " +
                                                    "attribute. Resources decorated with a RESTar attribute " +
                                                    "are registered automatically");
            var type = typeof(T);
            var availableMethods = new[] {method}.Union(addMethods).ToList();
            Resource<T>.Make(availableMethods, operationsProvider);
        }

        private static ICollection<RESTarMethods> GetAvailableMethods(Type resource)
        {
            if (resource == null)
                return null;
            if (resource.HasAttribute<DynamicTableAttribute>())
                return RESTarConfig.Methods;
            return resource.GetAttribute<RESTarAttribute>()?.AvailableMethods;
        }

        private static ICollection<RESTarOperations> NecessaryOpDefs(IEnumerable<RESTarMethods> restMethods)
        {
            return restMethods.SelectMany(method =>
            {
                switch (method)
                {
                    case RESTarMethods.GET:
                        return new[] {RESTarOperations.Select};
                    case RESTarMethods.POST:
                        return new[] {RESTarOperations.Insert};
                    case RESTarMethods.PUT:
                        return new[] {RESTarOperations.Select, RESTarOperations.Insert, RESTarOperations.Update};
                    case RESTarMethods.PATCH:
                        return new[] {RESTarOperations.Select, RESTarOperations.Update};
                    case RESTarMethods.DELETE:
                        return new[] {RESTarOperations.Select, RESTarOperations.Delete};
                    case RESTarMethods.Private_GET:
                        return new[] {RESTarOperations.Select};
                    case RESTarMethods.Private_POST:
                        return new[] {RESTarOperations.Insert};
                    case RESTarMethods.Private_PUT:
                        return new[] {RESTarOperations.Select, RESTarOperations.Insert, RESTarOperations.Update};
                    case RESTarMethods.Private_PATCH:
                        return new[] {RESTarOperations.Select, RESTarOperations.Update};
                    case RESTarMethods.Private_DELETE:
                        return new[] {RESTarOperations.Select, RESTarOperations.Delete};
                }
                return null;
            }).Distinct().ToList();
        }
    }
}