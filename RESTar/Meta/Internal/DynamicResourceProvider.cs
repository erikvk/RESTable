using System;
using System.Collections.Generic;
using System.Linq;
using Dynamit;
using RESTar.Internal;
using RESTar.Linq;
using RESTar.Resources;
using RESTar.Resources.Operations;
using RESTar.Results;
using Starcounter;

namespace RESTar.Meta.Internal
{
    internal class DynamicResourceProvider : EntityResourceProvider<DDictionary>
    {
        internal override bool Include(Type type) => false;
        internal override void MakeClaimRegular(IEnumerable<Type> types) { }
        internal override void MakeClaimWrapped(IEnumerable<Type> types) { }
        internal override void Validate() { }
        protected override Type AttributeType { get; } = null;
        public override Selector<T> GetDefaultSelector<T>() => DDictionaryOperations<T>.Select;
        public override Inserter<T> GetDefaultInserter<T>() => DDictionaryOperations<T>.Insert;
        public override Updater<T> GetDefaultUpdater<T>() => DDictionaryOperations<T>.Update;
        public override Deleter<T> GetDefaultDeleter<T>() => DDictionaryOperations<T>.Delete;
        public override Counter<T> GetDefaultCounter<T>() => null;
        public override Profiler<T> GetProfiler<T>() => DDictionaryOperations<T>.Profile;

        internal void InsertTable(Admin.Resource resource)
        {
            DynamicResource dynamicResource = null;
            Db.TransactAsync(() =>
            {
                var newTable = DynamitControl.DynamitTypes.FirstOrDefault(type =>
                                   Db.SQL<DynamicResource>(DynamicResource.ByTableName, type.RESTarTypeName()).FirstOrDefault() == null)
                               ?? throw new NoAvailableDynamicTable();
                if (!string.IsNullOrWhiteSpace(resource.Alias))
                    new Admin.ResourceAlias
                    {
                        Alias = resource.Alias,
                        Resource = resource.Name
                    };
                dynamicResource = new DynamicResource(resource.Name, newTable, resource.EnabledMethods, resource.Description);
            });
            CreateDynamicResource(dynamicResource);
        }

        internal void RegisterDynamicResources() => Db
            .SQL<DynamicResource>(DynamicResource.All)
            .ForEach(CreateDynamicResource);

        internal bool RemoveDynamicResource(DynamicResource dynamicResource, IResource resource)
        {
            if (dynamicResource == null) return false;
            DynamitControl.ClearTable(dynamicResource.TableName);
            var alias = Admin.ResourceAlias.GetByResource(dynamicResource.Name);
            Db.TransactAsync(() =>
            {
                alias?.Delete();
                dynamicResource.Delete();
            });
            RemoveResource(resource);
            return true;
        }

        internal void CreateDynamicResource(DynamicResource resource)
        {
            if (resource.Table == null)
            {
                Db.TransactAsync(resource.Delete);
                return;
            }
            InsertResource
            (
                type: resource.Table,
                fullName: resource.Name,
                attribute: resource.Attribute
            );
        }
    }
}