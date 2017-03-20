using System;
using System.Collections.Generic;
using System.Linq;
using Starcounter;

namespace RESTar.Internal
{
    [Database]
    public class DynamicResource : IResource
    {
        public string Name { get; }
        public bool Editable { get; }

        public ICollection<RESTarMethods> AvailableMethods
        {
            get { return AvailableMethodsString.ToMethodsList(); }
            set { AvailableMethodsString = value.ToMethodsString(); }
        }

        public string AvailableMethodsString { get; private set; }
        public Type TargetType => DynamitControl.GetByTableName(Name);
        public string Alias => ResourceAlias.ByResource(TargetType);
        public long? NrOfEntities => DB.RowCount(Name);

        public DynamicResource(Type table, ICollection<RESTarMethods> availableMethods)
        {
            Name = table.FullName;
            Editable = true;
            AvailableMethods = availableMethods;
        }

        public static DynamicResource Make(Resource resource)
        {
            DynamicResource dynamicResource = null;
            Db.TransactAsync(() =>
            {
                var newTable = DynamitControl.DynamitTypes.FirstOrDefault(ResourceAlias.NotExists);
                if (newTable == null)
                    throw new NoAvalailableDynamicTableException();
                new ResourceAlias
                {
                    Alias = resource.Alias,
                    Resource = newTable.FullName
                };
                dynamicResource = new DynamicResource(newTable, resource.AvailableMethods);
            });
            RESTarConfig.AddResource(dynamicResource);
            return dynamicResource;
        }

        public static void Delete(Resource resource)
        {
            var iresource = RESTarConfig.NameResources[resource.Name.ToLower()];
            if (!(iresource is DynamicResource)) return;
            DynamitControl.ClearTable(iresource.Name);
            RESTarConfig.RemoveResource(iresource);
            var alias = DB.Get<ResourceAlias>("Resource", iresource.TargetType.FullName);
            Db.TransactAsync(() => alias?.Delete());
            Db.TransactAsync(() => (iresource as DynamicResource)?.Delete());
        }

        public IEnumerable<object> Select(IRequest request) =>
            DDictionaryOperations.Selector().Select(request);

        public int Insert(IEnumerable<object> entities, IRequest request) =>
            DDictionaryOperations.Inserter().Insert((dynamic) entities, request);

        public int Update(IEnumerable<object> entities, IRequest request) =>
            DDictionaryOperations.Updater().Update((dynamic) entities, request);

        public int Delete(IEnumerable<object> entities, IRequest request) =>
            DDictionaryOperations.Deleter().Delete((dynamic) entities, request);

    }
}