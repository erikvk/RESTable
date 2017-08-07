using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using RESTar.Admin;
using RESTar.Serialization;
using static RESTar.Methods;

namespace RESTar
{
    /// <summary>
    /// The counter resource returns the entity count for a given resource
    /// </summary>
    [RESTar(GET, Singleton = true)]
    public class Counter : Dictionary<string, int>, ISelector<Counter>
    {
        /// <summary>
        /// RESTar selector (don't use)
        /// </summary>
        public IEnumerable<Counter> Select(IRequest<Counter> request)
        {
            if (request.Body == null)
                throw new Exception("Missing data source for count operation");
            switch (JToken.Parse(request.Body))
            {
                case JArray array: return new[] {new Counter {["Count"] = array.Count}};
                case JObject jobj:
                    var uriToken = jobj.SafeGetNoCase("uri");
                    if (uriToken?.Type != JTokenType.String)
                        throw new Exception("Invalid source URI");
                    var uri = uriToken.Value<string>();
                    var response = HTTP.Internal(GET, new Uri(uri, UriKind.Relative), request.AuthToken);
                    if (response?.IsSuccessStatusCode != true)
                        throw new Exception(
                            $"Could not get source data from '<self>:{Settings._Port}{Settings._Uri}{uri}'. " +
                            $"{response?.StatusCode}: {response?.StatusDescription}. {response?.Headers["ErrorInfo"]}");
                    if (response.StatusCode == 204 || string.IsNullOrEmpty(response.Body))
                        return new[] {new Counter {["Count"] = 0}};
                    var items = response.Body.Deserialize<List<dynamic>>();
                    var count = items.Count;
                    return new[] {new Counter {["Count"] = count}};
                default: return null;
            }
        }
    }
}