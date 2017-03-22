using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Starcounter;
using static RESTar.RESTarPresets;

namespace RESTar
{
    [RESTar(ReadOnly)]
    public class Counter : Dictionary<string, int>, ISelector<Counter>
    {
        public IEnumerable<Counter> Select(IRequest request)
        {
            if (request.Json == null)
                throw new Exception("Missing data source for operation");
            var jtoken = JToken.Parse(request.Json);
            var array = jtoken as JArray;
            var obj = jtoken as JObject;
            if (array != null)
                return new[] {new Counter {["Count"] = array.Count}};
            if (obj == null)
                return null;
            var uriToken = obj.FirstOrDefault<KeyValuePair<string, JToken>>(prop => prop.Key.ToLower() == "uri");
            if (uriToken.Value?.Type != JTokenType.String)
                throw new Exception("Invalid source URI");
            var uri = uriToken.Value.Value<string>().ParseSelfUri();
            var response = Self.GET(uri.port, uri.path);
            if (response?.IsSuccessStatusCode != true)
                throw new Exception($"Could not get source data from '{uri}'");
            if (response.StatusCode == 204 || string.IsNullOrEmpty(response.Body))
                return new[] {new Counter {["Count"] = 0}};
            IEnumerable<object> items = response.Body.DeserializeDyn();
            var count = items.Count();
            return new[] {new Counter {["Count"] = count}};
        }
    }
}