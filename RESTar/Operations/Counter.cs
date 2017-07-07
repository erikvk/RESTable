using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace RESTar.Operations
{
    /// <summary>
    /// The counter resource returns the entity count for a given resource
    /// </summary>
    [RESTar(RESTarPresets.ReadOnly, Singleton = true)]
    public class Counter : Dictionary<string, int>, ISelector<Counter>
    {
        /// <summary>
        /// RESTar selector (don't use)
        /// </summary>
        public IEnumerable<Counter> Select(IRequest<Counter> request)
        {
            if (request.Body == null)
                throw new Exception("Missing data source for operation");
            var jtoken = JToken.Parse(request.Body);
            var array = jtoken as JArray;
            var obj = jtoken as JObject;
            if (array != null)
                return new[] {new Counter {["Count"] = array.Count}};
            if (obj == null)
                return null;
            var uriToken = obj.FirstOrDefault<KeyValuePair<string, JToken>>(prop => prop.Key.ToLower() == "uri");
            if (uriToken.Value?.Type != JTokenType.String)
                throw new Exception("Invalid source URI");
            var uri = uriToken.Value.Value<string>();
            var response = HTTP.InternalRequest(RESTarMethods.GET, new Uri(uri, UriKind.Relative), request.AuthToken);
            if (response?.IsSuccessStatusCode != true)
                throw new Exception($"Could not get source data from '<self>:{Settings._Port}{Settings._Uri}{uri}'. " +
                                    $"{response?.StatusCode}: {response?.StatusDescription}. {response?.Headers["ErrorInfo"]}");
            if (response.StatusCode == 204 || string.IsNullOrEmpty(response.Body))
                return new[] {new Counter {["Count"] = 0}};
            IEnumerable<object> items = response.Body.Deserialize<dynamic>();
            var count = items.Count();
            return new[] {new Counter {["Count"] = count}};
        }
    }
}