using System;
using System.Linq;
using Dynamit;
using RESTar.Operations;
using Starcounter;
using static System.StringComparison;
using static RESTar.Internal.Transactions;

namespace RESTar.Internal
{
    /// <summary>
    /// Creates and structures all the dynamic resources for this RESTar instance
    /// </summary>
    [Database]
    public class DynamicResource : IResource<DDictionary>
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
        public Selector<DDictionary> Select => DDictionaryOperations.Select;

        /// <summary>
        /// RESTar inserter (don't use)
        /// </summary>
        public Inserter<DDictionary> Insert => (e, r) => DDictionaryOperations.Insert(e, r);

        /// <summary>
        /// RESTar updater (don't use)
        /// </summary>
        public Updater<DDictionary> Update => (e, r) => DDictionaryOperations.Update(e, r);

        /// <summary>
        /// RESTar deleter (don't use)
        /// </summary>
        public Deleter<DDictionary> Delete => (e, r) => DDictionaryOperations.Delete(e, r);

        /// <summary>
        /// Compares two dynamic resources for equality
        /// </summary>
        public bool Equals(IResource x, IResource y) => x.Name == y.Name;

        /// <summary>
        /// Gets the hashcode for a dynamic resource
        /// </summary>
        public int GetHashCode(IResource obj) => obj.Name.GetHashCode();

        /// <summary>
        /// Compares two dynamic resources
        /// </summary>
        public int CompareTo(IResource other) => string.Compare(Name, other.Name, Ordinal);

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
            var dynamicResource = Trans(() =>
            {
                var newTable = DynamitControl.DynamitTypes.FirstOrDefault(ResourceAlias.NotExists);
                if (newTable == null)
                    throw new NoAvalailableDynamicTableException();
                new ResourceAlias
                {
                    Alias = resource.Alias,
                    Resource = newTable.FullName
                };
                return new DynamicResource
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
            var dynamicResource = resource.IResource as DynamicResource;
            if (dynamicResource == null) return;
            DynamitControl.ClearTable(dynamicResource.Name);
            RESTarConfig.RemoveResource(dynamicResource);
            var alias = DB.Get<ResourceAlias>("Resource", dynamicResource.TargetType.FullName);
            Trans(() =>
            {
                alias?.Delete();
                Db.Delete(dynamicResource);
            });
        }
    }
}