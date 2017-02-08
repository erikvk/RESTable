using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Dynamit;

namespace RESTar
{
    [RESTar(RESTarMethods.GET)]
    public class Schema : Dictionary<string, string>, ISelector<Schema>
    {
        public string resource { private get; set; }

        public IEnumerable<Schema> Select(IRequest request)
        {
            var validCondition = request.GetCondition("resource")?.Value as string;
            if (validCondition == null)
                throw new AbortedSelectorException("Invalid request: No valid 'resource' argument. " +
                                                   "Example: /schema/resource=my_resource_name");
            var _resource = validCondition.FindResource();
            if (_resource.IsSubclassOf(typeof(DDictionary)))
                return null;
            var properties = _resource.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            var schema = new Schema();
            foreach (var property in properties)
            {
                if (!property.HasAttribute<IgnoreDataMemberAttribute>())
                {
                    var alias = property.GetAttribute<DataMemberAttribute>()?.Name;
                    schema[alias ?? property.Name] = property.PropertyType.FullName;
                }
            }
            return new[] {schema};
        }
    }
}