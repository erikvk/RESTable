using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using RESTar.Admin;
using Starcounter;
using static RESTar.Operations.Transact;

namespace RESTar.Internal
{
    /// <summary>
    /// Creates and structures all the dynamic resources for this RESTar instance
    /// </summary>
    [Database]
    public class DynamicResource
    {
        /// <summary>
        /// The available methods for this resource
        /// </summary>
        public IReadOnlyList<Methods> AvailableMethods
        {
            get => AvailableMethodsString.ToMethodsArray();
            set => AvailableMethodsString = value.ToMethodsString();
        }

        /// <summary>
        /// The name of this resource
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// A string representation of the available REST methods
        /// </summary>
        [IgnoreDataMember]
        public string AvailableMethodsString { get; private set; }

        /// <summary>
        /// The target type for this resource
        /// </summary>
        public Type Table => DynamitControl.GetByTableName(Name);

        internal RESTarAttribute Attribute => new RESTarAttribute
            (AvailableMethods.OrderBy(i => i, MethodComparer.Instance).ToList())
            {
                AllowDynamicConditions = true,
                Singleton = false,
                Editable = true
            };

        private static readonly string SQL = $"SELECT t FROM {typeof(DynamicResource).FullName} t";
        internal static IEnumerable<DynamicResource> All => Db.SQL<DynamicResource>(SQL);

        private DynamicResource(Type table, IEnumerable<Methods> availableMethods)
        {
            Name = table.FullName;
            var methods = availableMethods.Distinct().ToList();
            methods.Sort(MethodComparer.Instance);
            AvailableMethods = methods;
        }

        internal static void MakeTable(Admin.Resource resource)
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
                    resource.AvailableMethods
                );
            });
            Resource.AutoMakeDynamicResource(dynamicResource);
        }

        internal static void DeleteTable(Admin.Resource resource)
        {
            var dynamicResource = Resource.GetDynamicResource(resource.Name);
            if (dynamicResource == null) return;
            DynamitControl.ClearTable(dynamicResource.Name);
            var alias = ResourceAlias.ByResource(dynamicResource.Table);

            Trans(() =>
            {
                alias?.Delete();
                dynamicResource.Delete();
            });
            RESTarConfig.RemoveResource(resource.IResource);
        }
    }
}