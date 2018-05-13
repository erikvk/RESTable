using System;
using System.Collections.Generic;
using System.Linq;
using RESTar.Meta.Internal;
using RESTar.Resources;
using RESTar.Results;
using Starcounter;

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
        internal const string ByName = All + " WHERE t.Name =?";

        /// <summary>
        /// The available methods for this resource
        /// </summary>
        public IReadOnlyList<Method> AvailableMethods
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
        [RESTarMember(ignore: true)] public string AvailableMethodsString { get; private set; }

        /// <summary/>
        [RESTarMember(ignore: true), Obsolete] public bool Editable { get; internal set; }

        /// <summary/>
        [RESTarMember(ignore: true), Obsolete] public bool Visible { get; internal set; }

        /// <summary/>
        [RESTarMember(ignore: true), Obsolete] public string EntityViewHtml { get; internal set; }

        /// <summary/>
        [RESTarMember(ignore: true), Obsolete] public string EntitiesViewHtml { get; internal set; }

        /// <summary/>
        [RESTarMember(ignore: true), Obsolete] public bool Viewable { get; internal set; }

        /// <summary/>
        [RESTarMember(ignore: true), Obsolete] public bool IsViewable { get; internal set; }

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

        private DynamicResource(string name, Type table, IEnumerable<Method> availableMethods, string description = null)
        {
            Name = name;
            TableName = table.RESTarTypeName();
            Description = description;
            var methods = availableMethods.Distinct().ToList();
            methods.Sort(MethodComparer.Instance);
            AvailableMethods = methods;
        }

        internal static void MakeTable(Admin.Resource resource)
        {
            DynamicResource dynamicResource = null;
            Db.TransactAsync(() =>
            {
                var newTable = DynamitControl.DynamitTypes.FirstOrDefault(type => !Exists(type.RESTarTypeName()))
                               ?? throw new NoAvailableDynamicTable();
                if (!string.IsNullOrWhiteSpace(resource.Alias))
                    new Admin.ResourceAlias
                    {
                        Alias = resource.Alias,
                        Resource = resource.Name
                    };
                dynamicResource = new DynamicResource(resource.Name, newTable, resource.EnabledMethods, resource.Description);
            });
            ResourceFactory.MakeDynamicResource(dynamicResource);
        }

        internal static DynamicResource Get(string resourceName) => Db
            .SQL<DynamicResource>(ByName, resourceName).FirstOrDefault();

        internal static bool DeleteTable(Admin.Resource resource)
        {
            var dynamicResource = Get(resource.Name);
            if (dynamicResource == null) return false;
            DynamitControl.ClearTable(dynamicResource.TableName);
            var alias = Admin.ResourceAlias.GetByResource(dynamicResource.Name);
            Db.TransactAsync(() =>
            {
                alias?.Delete();
                dynamicResource.Delete();
            });
            RESTarConfig.RemoveResource(resource.IResource);
            return true;
        }
    }
}