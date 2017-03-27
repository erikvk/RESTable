using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Starcounter;

namespace RESTar
{
    [RESTar(RESTarPresets.ReadOnly, Dynamic = true)]
    public class SetOperations : Dictionary<string, dynamic>, ISelector<SetOperations>
    {
        public IEnumerable<SetOperations> Select(IRequest request)
        {
            if (request.Json == null)
                throw new Exception("Missing data source for operation");
            var jobject = JObject.Parse(request.Json);
            Func<JToken, JArray> treeRecursor = null;
            treeRecursor = token =>
            {
                if (treeRecursor == null)
                    throw new ArgumentNullException(nameof(treeRecursor));
                var value = token as JValue;
                var obj = token as JObject;

                if (value != null)
                {
                    if (value.Type != JTokenType.String)
                        throw new Exception($"Invalid type '{value.Type}' for set operation argument " +
                                            $"'{value.ToString(CultureInfo.InvariantCulture)}'. Must be string.");
                    var str = value.Value<string>();
                    if (string.IsNullOrEmpty(str))
                        throw new Exception("Operation arguments cannot be empty strings");
                    string json;
                    var first = str.First();
                    if (first == '[')
                        json = str;
                    else if (char.IsDigit(first) || first == '/')
                    {
                        var uri = str.ParseSelfUri();
                        var response = Self.GET(uri.port, uri.path);
                        if (response?.IsSuccessStatusCode != true)
                            throw new Exception($"Could not get source data from '{uri}'");
                        if (response.StatusCode == 204 || string.IsNullOrEmpty(response.Body))
                            json = "[]";
                        else json = response.Body;
                    }
                    else
                        throw new Exception($"Invalid string '{str}'. Must be a REST request URI " +
                                            $"beginning with '{Settings._Uri}/<resource locator>' or " +
                                            $"a JSON array.");
                    return JsonConvert.DeserializeObject<JArray>(json, Serializer.JsonNetSettings);
                }

                if (obj != null)
                {
                    var prop = obj.Properties().FirstOrDefault();
                    if (obj.Count != 1 || prop?.Value?.Type != JTokenType.Array)
                        throw new Exception("Set operation objects must contain one and only one property where " +
                                            "the name is a set operation and the value is a list of strings and/or " +
                                            "objects.");
                    var arr = prop.Value.Value<JArray>();
                    switch (prop.Name.ToLower())
                    {
                        case "distinct":
                            if (arr.Count != 1)
                                throw new Exception("Distinct takes one and only one set as operand");
                            return Distinct(treeRecursor(arr.First));
                        case "except":
                            if (arr.Count != 2)
                                throw new Exception("Except takes two and only two argument sets as operands");
                            return Except(treeRecursor(arr[0]), treeRecursor(arr[1]));
                        case "intersect":
                            if (arr.Count < 2)
                                throw new Exception("Intersect takes at least two sets as operands");
                            return Intersect(arr.Select(treeRecursor).ToArray());
                        case "union":
                            if (arr.Count < 2)
                                throw new Exception("Union takes at least two sets as operands");
                            return Union(arr.Select(treeRecursor).ToArray());
                        case "map":
                            if (arr.Count != 2)
                                throw new Exception("Map takes two and only two arguments");
                            return Map(treeRecursor(arr[0]), (string) arr[1]);
                        default:
                            throw new ArgumentOutOfRangeException($"Unknown operation '{prop.Name}'. " +
                                                                  "Avaliable operations: distinct, except, " +
                                                                  "intersect, union.");
                    }
                }
                throw new ArgumentException($"Invalid type '{token.Type}' in operations tree");
            };

            var results = JsonConvert.SerializeObject(treeRecursor(jobject), Serializer.JsonNetSettings);
            try
            {
                return results.Deserialize<IEnumerable<SetOperations>>();
            }
            catch
            {
                throw new Exception("JSON format error");
            }
        }

        private static JArray Distinct(JArray array)
        {
            if (array == null) return null;
            if (!array.Any()) return new JArray();
            var output = new JArray();
            foreach (var item in array)
                if (!output.Any(i => JToken.DeepEquals(item, i)))
                    output.Add(item);
            return output;
        }

        private static JArray Intersect(params JArray[] arrays)
        {
            if (arrays == null || arrays.Length < 2)
                throw new ArgumentException(nameof(arrays));
            var output = new JArray();
            var orderedByCount = arrays.OrderBy(arr => arr.Count);
            var shortest = Distinct(orderedByCount.First());
            var others = orderedByCount.Skip(1).ToList();
            foreach (var item in shortest)
                if (others.All(array => array.Any(_item => JToken.DeepEquals(item, _item))))
                    output.Add(item);
            return Distinct(output);
        }

        private static JArray Union(params JArray[] arrays)
        {
            if (arrays == null || arrays.Length < 2)
                throw new ArgumentException(nameof(arrays));
            var output = new JArray();
            foreach (var array in arrays)
            {
                foreach (var item in array)
                {
                    if (!output.Any(i => JToken.DeepEquals(item, i)))
                        output.Add(item);
                }
            }
            return Distinct(output);
        }

        private static JArray Except(JArray array1, JArray array2)
        {
            if (array1 == null) throw new ArgumentException(nameof(array1));
            if (array2 == null) throw new ArgumentException(nameof(array2));
            array1 = Distinct(array1);
            var toRemove = (from item2 in array2
                from item1 in array1
                where JToken.DeepEquals(item1, item2)
                select item1).ToList();
            foreach (var item in toRemove)
                array1.Remove(item);
            return array1;
        }

        private static readonly Regex macroRegex = new Regex(@"\$\([^\$\(\)]+\)");

        private static JArray Map(JArray set, string mapper)
        {
            if (set == null) throw new ArgumentException(nameof(set));
            if (string.IsNullOrEmpty(mapper)) throw new ArgumentException(nameof(mapper));
            set = Distinct(set);
            var mapped = new JArray();
            var matches = macroRegex.Matches(mapper);
            foreach (var item in set)
            {
                var skip = false;
                var localMapper = new StringBuilder(mapper);
                var obj = item as JObject;
                if (obj == null)
                    throw new Exception("JSON syntax error in map set. Set must be of objects");
                foreach (var match in matches)
                {
                    var matchstring = match.ToString();
                    var key = matchstring.Substring(2, matchstring.Length - 3);
                    var asDict = (IDictionary<string, JToken>) obj;
                    if (asDict.ContainsKey(key))
                        localMapper.Replace(matchstring, WebUtility.UrlEncode(obj[key]?.ToString() ?? "null"));
                    else skip = true;
                }
                if (!skip)
                {
                    var uri = localMapper.ToString().ParseSelfUri();
                    var response = Self.GET(uri.port, uri.path);
                    if (response?.IsSuccessStatusCode != true)
                        throw new Exception($"Could not get source data from '{uri}'");
                    JArray toAdd;
                    if (response.StatusCode == 204 || string.IsNullOrEmpty(response.Body))
                        toAdd = new JArray {new JObject()};
                    else toAdd = JArray.Parse(response.Body);
                    foreach (var i in toAdd)
                        if (!mapped.Any(_i => JToken.DeepEquals(i, _i)))
                            mapped.Add(i);
                }
            }
            return mapped;
        }
    }
}