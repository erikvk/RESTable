using System;
using System.Collections.Generic;
using System.Linq;
using Dynamit;
using RESTar.Operations;
using Starcounter;

namespace RESTar.Internal
{
    [Database]
    public class DynamicResource : IResource
    {
        public string Name { get; }
        public bool Editable { get; }
        public bool IsDynamic => true;
        public string AvailableMethodsString { get; private set; }
        public Type TargetType => DynamitControl.GetByTableName(Name);
        public string Alias => ResourceAlias.ByResource(TargetType);
        public long? NrOfEntities => DB.RowCount(Name);

        public bool Equals(IResource x, IResource y) => x.Name == y.Name;
        public int GetHashCode(IResource obj) => obj.Name.GetHashCode();
        public int CompareTo(IResource other) => string.Compare(Name, other.Name, StringComparison.Ordinal);
        public override int GetHashCode() => Name.GetHashCode();

        public Selector<dynamic> Select => DynOperations.Select;
        public Inserter<dynamic> Insert => (e, r) => DynOperations.Insert((IEnumerable<DDictionary>) e, r);
        public Updater<dynamic> Update => (e, r) => DynOperations.Update((IEnumerable<DDictionary>) e, r);
        public Deleter<dynamic> Delete => (e, r) => DynOperations.Delete((IEnumerable<DDictionary>) e, r);

        public RESTarMethods[] AvailableMethods
        {
            get { return AvailableMethodsString.ToMethodsArray(); }
            private set { AvailableMethodsString = value.ToMethodsString(); }
        }

        private DynamicResource(Type table, RESTarMethods[] availableMethods)
        {
            Name = table.FullName;
            Editable = true;
            AvailableMethods = availableMethods;
        }

        public static DynamicResource MakeTable(Resource resource)
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

        public static void DeleteTable(Resource resource)
        {
            var iresource = RESTarConfig.NameResources[resource.Name.ToLower()];
            if (!(iresource is DynamicResource)) return;
            DynamitControl.ClearTable(iresource.Name);
            RESTarConfig.RemoveResource(iresource);
            var alias = DB.Get<ResourceAlias>("Resource", iresource.TargetType.FullName);
            Db.TransactAsync(() =>
            {
                alias?.Delete();
                Db.Delete((DynamicResource) iresource);
            });
        }
    }
}