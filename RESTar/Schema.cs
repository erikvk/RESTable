using System;
using System.Collections.Generic;
using RESTar.Deflection;
using static RESTar.Operators;

namespace RESTar
{
    /// <summary>
    /// Gets a schema for a given resource
    /// </summary>
    [RESTar(RESTarMethods.GET, Singleton = true)]
    public class Schema : Dictionary<string, string>, ISelector<Schema>
    {
        /// <summary>
        /// The name of the resource to get the schema for
        /// </summary>
        public string Resource { private get; set; }

        /// <summary>
        /// RESTar selector (don't use)
        /// </summary>
        public IEnumerable<Schema> Select(IRequest<Schema> request)
        {
            var validCondition = request.Conditions["resource", EQUALS]?.Value as string;
            if (validCondition == null)
                throw new Exception("Invalid resource argument, format: /schema/resource=my_resource_name");
            var schema = MakeSchema(validCondition);
            return new[] {schema};
        }

        internal static Schema MakeSchema(string resourceName)
        {
            var res = resourceName.FindResource();
            if (res.IsDDictionary) return null;
            var schema = new Schema();
            res.GetStaticProperties().Values.ForEach(p => schema[p.Name] = p.Type.FullName);
            return schema;
        }
    }
}