using System;
using System.Collections.Generic;
using System.Linq;
using RESTar.Resources;
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

        internal DynamicResource(string name, Type table, IEnumerable<Method> availableMethods, string description = null)
        {
            Name = name;
            TableName = table.RESTarTypeName();
            Description = description;
            var methods = availableMethods.Distinct().ToList();
            methods.Sort(MethodComparer.Instance);
            AvailableMethods = methods;
        }

        internal static DynamicResource Get(string resourceName) => Db.SQL<DynamicResource>(ByName, resourceName).FirstOrDefault();
    }
}