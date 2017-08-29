using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using RESTar.Serialization;
using static RESTar.Methods;
using JTokens = System.Collections.Generic.IEnumerable<Newtonsoft.Json.Linq.JToken>;

#pragma warning disable 1591

namespace RESTar
{
    /// <summary>
    /// The SetOperations resource can perform advanced operations on entities in one
    /// or more RESTar resources. See the RESTar Specification for details.
    /// </summary>
    [RESTar(GET, Singleton = true, Description = description)]
    public class SetOperations : JObject, ISelector<SetOperations>
    {
        private const string description = "The SetOperations resource can perform advanced operations " +
                                           "on entities in one or more RESTar resources. See the RESTar " +
                                           "Specification for details.";

        public SetOperations()
        {
        }

        private SetOperations(JObject other) : base(other)
        {
        }

        static SetOperations() => macroRegex = new Regex(@"\$\([^\$\(\)]+\)");

        /// <inheritdoc />
        public IEnumerable<SetOperations> Select(IRequest<SetOperations> request)
        {
            if (request.Body == null)
                throw new Exception("Missing data source for operation");
            var jobject = Parse(request.Body);

            JTokens recursor(JToken token)
            {
                switch (token)
                {
                    case JValue value:
                        if (value.Type != JTokenType.String)
                            throw new Exception($"Invalid type '{value.Type}' for set operation argument " +
                                                $"'{value.ToString(CultureInfo.InvariantCulture)}'. Must be string.");
                        var str = value.Value<string>();
                        if (string.IsNullOrEmpty(str))
                            throw new Exception("Operation arguments cannot be empty strings");
                        string json;
                        var first = str[0];
                        if (first == '[')
                            json = str;
                        else if (char.IsDigit(first) || first == '/')
                        {
                            var uri = str;
                            var response = HTTP.Internal(GET, new Uri(uri, UriKind.Relative),
                                request.AuthToken);
                            if (response?.IsSuccessStatusCode != true)
                                throw new Exception(
                                    $"Could not get source data from '{uri}'. " +
                                    $"{response?.StatusCode}: {response?.StatusDescription}. {response?.Headers["RESTar-info"]}");
                            if (response.StatusCode == 204 || string.IsNullOrEmpty(response.Body))
                                json = "[]";
                            else json = response.Body;
                        }
                        else
                            throw new Exception($"Invalid string '{str}'. Must be a relative REST request URI " +
                                                "beginning with '/<resource locator>' or a JSON array.");
                        return json.Deserialize<JArray>();
                    case JObject obj:
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
                                return Distinct(recursor(arr.First));
                            case "except":
                                if (arr.Count != 2)
                                    throw new Exception("Except takes two and only two argument sets as operands");
                                return Except(recursor(arr[0]), recursor(arr[1]));
                            case "intersect":
                                if (arr.Count < 2)
                                    throw new Exception("Intersect takes at least two sets as operands");
                                return Intersect(arr.Select(recursor).ToArray());
                            case "union":
                                if (arr.Count < 2)
                                    throw new Exception("Union takes at least two sets as operands");
                                return Union(arr.Select(recursor).ToArray());
                            case "map":
                                if (arr.Count != 2)
                                    throw new Exception("Map takes two and only two arguments");
                                return Map(recursor(arr[0]), (string) arr[1], request);
                            default:
                                throw new ArgumentOutOfRangeException($"Unknown operation '{prop.Name}'. " +
                                                                      "Avaliable operations: distinct, except, " +
                                                                      "intersect, union.");
                        }
                    default: throw new ArgumentException($"Invalid type '{token.Type}' in operations tree");
                }
            }

            return recursor(jobject).Select(token =>
            {
                switch (token)
                {
                    case JValue value: return new SetOperations(new JObject(new JProperty("Value", value)));
                    case JObject @object: return new SetOperations(@object);
                    default: throw new Exception("Invalid entity type in set operation");
                }
            });
        }

        private static JTokens Distinct(JTokens array) => array?.Distinct(EqualityComparer);

        private static JTokens Intersect(params JTokens[] arrays) => Checked(arrays)
            .Aggregate((x, y) => x.Intersect(y, EqualityComparer));

        private static JTokens Union(params JTokens[] arrays) => Checked(arrays)
            .Aggregate((x, y) => x.Union(y, EqualityComparer));

        private static JTokens Except(params JTokens[] arrays) => Checked(arrays)
            .Aggregate((x, y) => x.Except(y, EqualityComparer));

        private static readonly Regex macroRegex;

        private static JTokens Map(JTokens set, string mapper, IRequest request)
        {
            if (set == null)
                throw new ArgumentException(nameof(set));
            if (string.IsNullOrEmpty(mapper))
                throw new ArgumentException(nameof(mapper));
            var mapped = new HashSet<JToken>(EqualityComparer);
            foreach (var item in Distinct(set))
            {
                var skip = false;
                var localMapper = new StringBuilder(mapper);
                if (!(item is IDictionary<string, JToken> dict))
                    throw new Exception("JSON syntax error in map set. Set must be of objects");
                foreach (var match in macroRegex.Matches(mapper))
                {
                    var matchstring = match.ToString();
                    var key = matchstring.Substring(2, matchstring.Length - 3);
                    if (dict.ContainsKey(key))
                    {
                        var value = dict[key]?.ToString() ?? "null";
                        if (value == "") value = "\"\"";
                        localMapper.Replace(matchstring, WebUtility.UrlEncode(value) ?? "null");
                    }
                    else skip = true;
                }
                if (!skip)
                {
                    var uri = localMapper.ToString();
                    var response = HTTP.Internal(GET, new Uri(uri, UriKind.Relative), request.AuthToken);
                    if (response?.IsSuccessStatusCode != true)
                        throw new Exception(
                            $"Could not get source data from '{uri}'. " +
                            $"{response?.StatusCode}: {response?.StatusDescription}. {response?.Headers["RESTar-info"]}");
                    if (response.StatusCode == 204 || string.IsNullOrEmpty(response.Body))
                        mapped.Add(new JObject());
                    else Serializer.Populate(response.Body, mapped);
                }
            }
            return mapped;
        }

        private static JTokens[] Checked(JTokens[] arrays) =>
            arrays == null || arrays.Length < 2 || arrays.Any(a => a == null)
                ? throw new ArgumentException(nameof(arrays))
                : arrays;
    }
}