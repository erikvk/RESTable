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
        public bool IsDDictionary => true;
        public string AvailableMethodsString { get; private set; }
        public Type TargetType => DynamitControl.GetByTableName(Name);
        public string Alias => ResourceAlias.ByResource(TargetType);
        public long? NrOfEntities => DB.RowCount(Name);
        public bool IsViewable { get; }
        public bool IsSingleton => false;
        public string AliasOrName => Alias ?? Name;
        public override string ToString() => AliasOrName;
        public bool IsStarcounterResource => true;
        public bool IsDynamic => true;
        public bool DynamicConditionsAllowed => true;
        public bool Equals(IResource x, IResource y) => x.Name == y.Name;
        public int GetHashCode(IResource obj) => obj.Name.GetHashCode();
        public int CompareTo(IResource other) => string.Compare(Name, other.Name, StringComparison.Ordinal);
        public override int GetHashCode() => Name.GetHashCode();
        public bool RequiresValidation => false;
        public RESTarResourceType ResourceType => RESTarResourceType.Dynamic;

        public Selector<dynamic> Select => DDictionaryOperations.Select;
        public Inserter<dynamic> Insert => (e, r) => DDictionaryOperations.Insert((IEnumerable<DDictionary>) e, r);
        public Updater<dynamic> Update => (e, r) => DDictionaryOperations.Update((IEnumerable<DDictionary>) e, r);
        public Deleter<dynamic> Delete => (e, r) => DDictionaryOperations.Delete((IEnumerable<DDictionary>) e, r);

        public RESTarMethods[] AvailableMethods
        {
            get => AvailableMethodsString.ToMethodsArray();
            private set => AvailableMethodsString = value.ToMethodsString();
        }

        private DynamicResource(Type table, RESTarMethods[] availableMethods, bool visible)
        {
            Name = table.FullName;
            Editable = true;
            AvailableMethods = availableMethods;
            IsViewable = visible;
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
                dynamicResource = new DynamicResource
                (
                    newTable,
                    resource.AvailableMethods,
                    resource.Visible
                );
            });
            RESTarConfig.AddResource(dynamicResource);
            return dynamicResource;
        }

        public static void DeleteTable(Resource resource)
        {
            var iresource = RESTarConfig.ResourceByName[resource.Name.ToLower()];
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