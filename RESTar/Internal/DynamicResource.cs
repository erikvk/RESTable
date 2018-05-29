using System;
using System.Collections.Generic;
using System.Linq;
using RESTar.Resources;
using Starcounter;

namespace RESTar.Internal
{
    /// <inheritdoc />
    /// <summary>
    /// Creates and structures all the dynamic resources for this RESTar instance
    /// </summary>
    [Database]
    public class DynamicResource : IProceduralEntityResource
    {
        internal const string All = "SELECT t FROM RESTar.Internal.DynamicResource t";
        internal const string ByTableName = All + " WHERE t.TableName =?";

        /// <inheritdoc />
        /// <summary>
        /// The available methods for this resource
        /// </summary>
        public Method[] Methods
        {
            get => AvailableMethodsString.ToMethodsArray();
            set => AvailableMethodsString = value.ToMethodsString();
        }

        /// <inheritdoc />
        /// <summary>
        /// The name of this resource
        /// </summary>
        public string Name { get; set; }

        /// <inheritdoc />
        /// <summary>
        /// The description for this resource
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The name of the dynamic table (used internally)
        /// </summary>
        public string TableName { get; }

        /// <summary>
        /// A string representation of the available REST methods
        /// </summary>
        [RESTarMember(ignore: true)] public string AvailableMethodsString { get; private set; }

        /// <inheritdoc />
        /// <summary />
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

        /// <inheritdoc />
        public Type Type => DynamitControl.GetByTableName(TableName);

        internal DynamicResource(string name, Type table, IEnumerable<Method> availableMethods, string description = null)
        {
            Name = name;
            TableName = table.RESTarTypeName();
            Description = description;
            Methods = availableMethods.ResolveMethodsCollection().ToArray();
        }
    }
}