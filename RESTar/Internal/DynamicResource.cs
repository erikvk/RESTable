using System;
using System.Collections.Generic;
using System.Linq;
using Dynamit;
using RESTar.Operations;
using Starcounter;

namespace RESTar.Internal
{
    /// <summary>
    /// Creates and structures all the dynamic resources for this RESTar instance
    /// </summary>
    [Database]
    public class DynamicResource : IResource
    {
        /// <summary>
        /// The name of this resource
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Is this resource editable?
        /// </summary>
        public bool Editable { get; }

        /// <summary>
        /// Is this a DDictionary resource?
        /// </summary>
        public bool IsDDictionary => true;

        /// <summary>
        /// A string representation of the available REST methods
        /// </summary>
        public string AvailableMethodsString { get; private set; }

        /// <summary>
        /// The target type for this resource
        /// </summary>
        public Type TargetType => DynamitControl.GetByTableName(Name);

        /// <summary>
        /// The alias of this resource (if any)
        /// </summary>
        public string Alias => ResourceAlias.ByResource(TargetType);

        /// <summary>
        /// The total number of entities in this resource
        /// </summary>
        public long? NrOfEntities => DB.RowCount(Name);

        /// <summary>
        /// Is this resource visible in the view?
        /// </summary>
        public bool IsViewable { get; }

        /// <summary>
        /// Is this a singleton resource?
        /// </summary>
        public bool IsSingleton => false;

        /// <summary>
        /// A friendly label for this resource
        /// </summary>
        public string AliasOrName => Alias ?? Name;

        /// <summary>
        /// Gets a string representation of this resource
        /// </summary>
        public override string ToString() => AliasOrName;

        /// <summary>
        /// Is this a Starcounter resource?
        /// </summary>
        public bool IsStarcounterResource => true;

        /// <summary>
        /// Does this resource contain dynamic members?
        /// </summary>
        public bool IsDynamic => true;

        /// <summary>
        /// Are runtime-defined conditions allowed for this resource?
        /// </summary>
        public bool DynamicConditionsAllowed => true;

        /// <summary>
        /// Compares two resource entities for equality
        /// </summary>
        public bool Equals(IResource x, IResource y) => x.Name == y.Name;

        /// <summary>
        /// Gets a hash code for this resource instance
        /// </summary>
        public int GetHashCode(IResource obj) => obj.Name.GetHashCode();

        /// <summary>
        /// Compares a resource entity to another
        /// </summary>
        public int CompareTo(IResource other) => string.Compare(Name, other.Name, StringComparison.Ordinal);

        /// <summary>
        /// Gets a hash code for this resource instance
        /// </summary>
        public override int GetHashCode() => Name.GetHashCode();

        /// <summary>
        /// Does this resource require validation on insertion and updating?
        /// </summary>
        public bool RequiresValidation => false;

        /// <summary>
        /// The RESTar resource type of this resource
        /// </summary>
        public RESTarResourceType ResourceType => RESTarResourceType.Dynamic;

        /// <summary>
        /// RESTar selector (don't use)
        /// </summary>
        public Selector<dynamic> Select => DDictionaryOperations.Select;

        /// <summary>
        /// RESTar inserter (don't use)
        /// </summary>
        public Inserter<dynamic> Insert => (e, r) => DDictionaryOperations.Insert((IEnumerable<DDictionary>) e, r);

        /// <summary>
        /// RESTar updater (don't use)
        /// </summary>
        public Updater<dynamic> Update => (e, r) => DDictionaryOperations.Update((IEnumerable<DDictionary>) e, r);

        /// <summary>
        /// RESTar deleter (don't use)
        /// </summary>
        public Deleter<dynamic> Delete => (e, r) => DDictionaryOperations.Delete((IEnumerable<DDictionary>) e, r);

        /// <summary>
        /// The available methods for this resource
        /// </summary>
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

        internal static DynamicResource MakeTable(Resource resource)
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

        internal static void DeleteTable(Resource resource)
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