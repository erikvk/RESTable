using System;
using System.Collections.Generic;
using Dynamit;
using static RESTar.Operators;

namespace RESTar
{
    [RESTar(RESTarMethods.GET)]
    public class Schema : Dictionary<string, string>, ISelector<Schema>
    {
        public string resource { private get; set; }

        public IEnumerable<Schema> Select(IRequest request)
        {
            var validCondition = request.Conditions["resource", EQUALS]?.Value as string;
            if (validCondition == null)
                throw new Exception("Invalid resource argument, format: /schema/resource=my_resource_name");
            var res = validCondition.FindResource();
            if (res.TargetType.IsSubclassOf(typeof(DDictionary))) return null;
            var s = new Schema();
            res.TargetType.GetPropertyList().ForEach(p => s[p.RESTarMemberName()] = p.PropertyType.FullName);
            return new[] {s};
        }
    }
}