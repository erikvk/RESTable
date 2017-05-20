using System;
using System.Collections.Generic;
using Dynamit;
using static RESTar.Operators;

namespace RESTar
{
    [RESTar(RESTarMethods.GET, Singleton = true, Viewable = true)]
    public class Schema : Dictionary<string, string>, ISelector<Schema>
    {
        [UniqueId]
        public string Resource { private get; set; }

        public IEnumerable<Schema> Select(IRequest request)
        {
            var validCondition = request.Conditions?["resource", EQUALS] as string;
            if (validCondition == null)
                throw new Exception("Invalid resource argument, format: /schema/resource=my_resource_name");
            var schema = MakeSchema(validCondition);
            return new[] {schema};
        }

        internal static Schema MakeSchema(string resourceName)
        {
            var res = resourceName.FindResource();
            if (res.TargetType.IsSubclassOf(typeof(DDictionary))) return null;
            var schema = new Schema();
            res.TargetType.GetPropertyList().ForEach(p => schema[p.RESTarMemberName()] = p.PropertyType.FullName);
            return schema;
        }
    }
}