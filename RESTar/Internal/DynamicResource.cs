using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
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
        public string Name { get; internal set; }

        /// <summary>
        /// The name of the dynamic table (used internally)
        /// </summary>
        public string TableName { get; internal set; }

        /// <summary>
        /// A string representation of the available REST methods
        /// </summary>
        [IgnoreDataMember]
        public string AvailableMethodsString { get; private set; }

        /// <summary>
        /// The target type for this resource
        /// </summary>
        public Type Table => DynamitControl.GetByTableName(TableName);

        internal RESTarAttribute Attribute => new RESTarAttribute
            (AvailableMethods.OrderBy(i => i, MethodComparer.Instance).ToList())
            {
                AllowDynamicConditions = true,
                Singleton = false,
                Editable = true
            };

        private static readonly string SQL = $"SELECT t FROM {typeof(DynamicResource).FullName} t";
        internal static IEnumerable<DynamicResource> All => Db.SQL<DynamicResource>(SQL);

        private static bool Exists(string tableName) =>
            Db.SQL<DynamicResource>($"{SQL} WHERE t.TableName =?", tableName).First != null;

        private DynamicResource(string name, Type table, IEnumerable<Methods> availableMethods)
        {
            Name = name;
            TableName = table.FullName;
            var methods = availableMethods.Distinct().ToList();
            methods.Sort(MethodComparer.Instance);
            AvailableMethods = methods;
        }

        internal static void MakeTable(Admin.Resource resource) => Resource.RegisterDynamicResource(Trans(() =>
        {
            var newTable = DynamitControl.DynamitTypes.FirstOrDefault(type => !Exists(type.FullName))
                           ?? throw new NoAvalailableDynamicTableException();
            if (!string.IsNullOrWhiteSpace(resource.Alias))
                new Admin.ResourceAlias
                {
                    Alias = resource.Alias,
                    Resource = resource.Name
                };
            return new DynamicResource(resource.Name, newTable, resource.EnabledMethods);
        }));

        internal static bool DeleteTable(Admin.Resource resource)
        {
            var dynamicResource = Resource.GetDynamicResource(resource.Name);
            if (dynamicResource == null) return false;
            DynamitControl.ClearTable(dynamicResource.TableName);
            var alias = Admin.ResourceAlias.ByResource(dynamicResource.Name);
            Trans(() =>
            {
                alias?.Delete();
                dynamicResource.Delete();
            });
            RESTarConfig.RemoveResource(resource.IResource);
            return true;
        }
    }
}