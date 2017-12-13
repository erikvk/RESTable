using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using RESTar.Resources;
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
        internal const string All = "SELECT t FROM RESTar.Internal.DynamicResource t";
        internal const string ByTableName = All + " WHERE t.TableName =?";

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
        /// The description for this resource
        /// </summary>
        public string Description { get; internal set; }

        /// <summary>
        /// The name of the dynamic table (used internally)
        /// </summary>
        public string TableName { get; }

        /// <summary>
        /// A string representation of the available REST methods
        /// </summary>
        [IgnoreDataMember] public string AvailableMethodsString { get; private set; }

        /// <summary>
        /// The target type for this resource
        /// </summary>
        public Type Table => DynamitControl.GetByTableName(TableName);

        internal RESTarAttribute Attribute => new RESTarAttribute(
            AvailableMethods.OrderBy(i => i, MethodComparer.Instance).ToList())
        {
            Singleton = false,
            Editable = true,
            Description = Description
        };

        internal static IEnumerable<DynamicResource> GetAll() => Db.SQL<DynamicResource>(All);
        private static bool Exists(string tableName) => Db.SQL<DynamicResource>(ByTableName, tableName).FirstOrDefault() != null;

        private DynamicResource(string name, Type table, IEnumerable<Methods> availableMethods,
            string description = null)
        {
            Name = name;
            TableName = table.FullName;
            Description = description;
            var methods = availableMethods.Distinct().ToList();
            methods.Sort(MethodComparer.Instance);
            AvailableMethods = methods;
        }

        internal static void MakeTable(Admin.Resource resource) => ResourceFactory.MakeDynamicResource(Trans(() =>
        {
            var newTable = DynamitControl.DynamitTypes.FirstOrDefault(type => !Exists(type.FullName))
                           ?? throw new NoAvalailableDynamicTableException();
            if (!string.IsNullOrWhiteSpace(resource.Alias))
                new Admin.ResourceAlias
                {
                    Alias = resource.Alias,
                    Resource = resource.Name
                };
            return new DynamicResource(resource.Name, newTable, resource.EnabledMethods, resource.Description);
        }));

        private const string DynamicResourceSQL = "SELECT t FROM RESTar.Internal.DynamicResource t WHERE t.Name =?";

        internal static DynamicResource Get(string resourceName) => Db
            .SQL<DynamicResource>(DynamicResourceSQL, resourceName).FirstOrDefault();

        internal static bool DeleteTable(Admin.Resource resource)
        {
            var dynamicResource = Get(resource.Name);
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