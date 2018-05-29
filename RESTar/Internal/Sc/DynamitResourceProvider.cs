using System;
using System.Collections.Generic;
using System.Linq;
using Dynamit;
using RESTar.Meta;
using RESTar.Resources;
using RESTar.Resources.Operations;
using RESTar.Results;
using Starcounter;

namespace RESTar.Internal.Sc
{
    internal class DynamitResourceProvider : EntityResourceProvider<DDictionary>, IProceduralEntityResourceProvider
    {
        internal const string ProviderId = "Dynamit";

        internal override bool Include(Type type)
        {
            if (type.IsWrapper())
                return type.GetWrappedType().IsSubclassOf(typeof(DDictionary)) && !type.HasResourceProviderAttribute();
            return type.IsSubclassOf(typeof(DDictionary)) && !type.HasResourceProviderAttribute();
        }

        internal override void Validate() { }

        // ReSharper disable once UnassignedGetOnlyAutoProperty
        protected override Type AttributeType { get; }

        protected override Selector<T> GetDefaultSelector<T>() => DDictionaryOperations<T>.Select;
        protected override Inserter<T> GetDefaultInserter<T>() => DDictionaryOperations<T>.Insert;
        protected override Updater<T> GetDefaultUpdater<T>() => DDictionaryOperations<T>.Update;
        protected override Deleter<T> GetDefaultDeleter<T>() => DDictionaryOperations<T>.Delete;
        protected override Counter<T> GetDefaultCounter<T>() => null;
        protected override Profiler<T> GetProfiler<T>() => DDictionaryOperations<T>.Profile;
        public override IDatabaseIndexer DatabaseIndexer { get; }
        internal DynamitResourceProvider(IDatabaseIndexer databaseIndexer) => DatabaseIndexer = databaseIndexer;

        protected override bool IsValid(IEntityResource resource, out string reason) =>
            StarcounterOperations<object>.IsValid(resource, out reason);

        public string BaseNamespace { get; } = "RESTar.Dynamic";

        private static bool Exists(Type type) => Db.SQL<DynamicResource>(DynamicResource.ByTableName, type.RESTarTypeName()).FirstOrDefault() != null;

        public IEnumerable<IProceduralEntityResource> Select() => Db
            .SQL<DynamicResource>(DynamicResource.All)
            .Where(resource =>
            {
                var resourceObjectLost = resource.Type == null;
                if (resourceObjectLost)
                {
                    Db.TransactAsync(resource.Delete);
                    return false;
                }
                return true;
            })
            .ToList();

        public IProceduralEntityResource Insert(string name, string description, Method[] methods, string alias)
        {
            DynamicResource proceduralResource = null;
            Db.TransactAsync(() =>
            {
                var newTable = DynamitControl.DynamitTypes.FirstOrDefault(type => !Exists(type)) ?? throw new NoAvailableDynamicTable();
                if (!string.IsNullOrWhiteSpace(alias))
                    new Admin.ResourceAlias {Alias = alias, Resource = name};
                proceduralResource = new DynamicResource(name, newTable, methods, description);
            });
            return proceduralResource;
        }

        public bool Update(IProceduralEntityResource resource, Func<bool> updater)
        {
            var success = false;
            Db.TransactAsync(() => success = updater());
            return success;
        }

        public bool Delete(IProceduralEntityResource resource)
        {
            var _resource = (DynamicResource) resource;
            DynamitControl.ClearTable(_resource.TableName);
            var alias = Admin.ResourceAlias.GetByResource(resource.Name);
            Db.TransactAsync(() =>
            {
                alias?.Delete();
                resource.Delete();
            });
            return true;
        }
    }
}