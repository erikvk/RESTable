using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using RESTable.ContentTypeProviders;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Resources.Operations;
using RESTable.Results;
using static System.StringComparison;
using static RESTable.Method;

#pragma warning disable 1591

namespace RESTable
{
    /// <inheritdoc cref="JObject" />
    /// <inheritdoc cref="RESTable.Resources.Operations.ISelector{T}" />
    /// <summary>
    /// The SetOperations resource can perform advanced operations on entities in one
    /// or more RESTable resources. See the RESTable Specification for details.
    /// </summary>
    [RESTable(GET, Description = description, AllowDynamicConditions = true)]
    public class SetOperations : JObject, IAsyncSelector<SetOperations>
    {
        private const string description = "The SetOperations resource can perform advanced operations " +
                                           "on entities in one or more RESTable resources. See the RESTable " +
                                           "Specification for details.";

        public SetOperations() { }
        private SetOperations(JObject other) : base(other) { }

        /// <inheritdoc />
        public async IAsyncEnumerable<SetOperations> SelectAsync(IRequest<SetOperations> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (!request.Body.HasContent)
                throw new Exception("Missing data source for SetOperations request");
            var jobject = await request.Body.Deserialize<JObject>().FirstOrDefaultAsync();

            async IAsyncEnumerable<JToken> recursor(JToken token)
            {
                switch (token)
                {
                    case JValue {Type: JTokenType.String} value:
                        var argument = value.Value<string>();
                        switch (argument?.FirstOrDefault())
                        {
                            case null: throw new Exception("Invalid operation expression. Expected string, found null");
                            case default(char): throw new Exception("Operation expressions cannot be empty strings");
                            case '[':
                            {
                                foreach (var arrayItem in JArray.Parse(argument))
                                    yield return arrayItem;
                                yield break;
                            }
                            case '/':
                            {
                                await using var innerRequest = request.Context.CreateRequest(argument);
                                var result = await request.Evaluate();
                                switch (result)
                                {
                                    case IEntities entities:
                                    {
                                        foreach (var item in JArray.FromObject(entities, JsonProvider.Serializer))
                                            yield return item;
                                        yield break;
                                    }
                                    case var other: throw new Exception($"Could not get source data from '{argument}'. {other.GetLogMessage().Result}");
                                }
                            }
                            default:
                                throw new Exception($"Invalid string '{argument}'. Must be a relative REST request URI " +
                                                    "beginning with '/<resource locator>' or a JSON array.");
                        }
                    case JObject {Count: 1, First: JProperty {Value: JArray arr} prop}:
                        switch (prop.Name.ToLower())
                        {
                            case "distinct":
                                if (arr.Count != 1)
                                    throw new Exception("Distinct takes one and only one set as operand");
                                await foreach (var item in Distinct(recursor(arr.First)))
                                    yield return item;
                                yield break;
                            case "except":
                                if (arr.Count != 2)
                                    throw new Exception("Except takes two and only two argument sets as operands");
                                await foreach (var item in Except(recursor(arr[0]), recursor(arr[1])))
                                    yield return item;
                                yield break;
                            case "intersect":
                            {
                                if (arr.Count < 2)
                                    throw new Exception("Intersect takes at least two sets as operands");

                                var tokens = new IAsyncEnumerable<JToken>[arr.Count];
                                for (var i = 0; i < tokens.Length; i += 1)
                                    tokens[i] = recursor(arr[i]);
                                await foreach (var item in Intersect(tokens))
                                    yield return item;
                                yield break;
                            }
                            case "union":
                            {
                                if (arr.Count < 2)
                                    throw new Exception("Union takes at least two sets as operands");
                                var tokens = new IAsyncEnumerable<JToken>[arr.Count];
                                for (var i = 0; i < tokens.Length; i += 1)
                                    tokens[i] = recursor(arr[i]);
                                await foreach (var item in Union(tokens))
                                    yield return item;
                                yield break;
                            }
                            case "map":
                                if (arr.Count != 2)
                                    throw new Exception("Map takes two and only two arguments");
                                await foreach (var item in Map(recursor(arr[0]), (string) arr[1], request))
                                    yield return item;
                                yield break;
                            default:
                                throw new ArgumentOutOfRangeException(
                                    $"Unknown operation '{prop.Name}'. Avaliable operations: distinct, except, " +
                                    "intersect, union.");
                        }
                    default:
                        throw new ArgumentException(
                            $"Invalid type '{token.Type}' in operations tree. Expected object with single array " +
                            "property, or string");
                }
            }

            await foreach (var token in recursor(jobject))
            {
                yield return token switch
                {
                    JValue value => new SetOperations(new JObject(new JProperty("Value", value))),
                    JObject @object => new SetOperations(@object),
                    _ => throw new Exception("Invalid entity type in set operation")
                };
            }
        }

        private static IAsyncEnumerable<JToken> Distinct(IAsyncEnumerable<JToken> array) => array?.Distinct(EqualityComparer);
        private static IAsyncEnumerable<JToken> Intersect(params IAsyncEnumerable<JToken>[] arrays) => Checked(arrays).Aggregate((x, y) => x.Intersect(y, EqualityComparer));
        private static IAsyncEnumerable<JToken> Union(params IAsyncEnumerable<JToken>[] arrays) => Checked(arrays).Aggregate((x, y) => x.Union(y, EqualityComparer));
        private static IAsyncEnumerable<JToken> Except(params IAsyncEnumerable<JToken>[] arrays) => Checked(arrays).Aggregate((x, y) => x.Except(y, EqualityComparer));

        private static async IAsyncEnumerable<JToken> Map(IAsyncEnumerable<JToken> set, string mapper, IRequest request)
        {
            if (set == null) throw new ArgumentException(nameof(set));
            if (string.IsNullOrEmpty(mapper)) throw new ArgumentException(nameof(mapper));

            var mapped = new HashSet<JToken>(EqualityComparer);
            var index = 0;
            var keys = new List<string>();
            mapper = Regex.Replace(mapper, RegEx.MapMacro, match =>
            {
                keys.Add(match.Groups["value"].Value);
                return $"{{{index++}}}";
            });
            var argumentCount = keys.Count;
            var valueBuffer = new object[argumentCount];

            await foreach (var item in Distinct(set))
            {
                var obj = item as JObject ?? throw new Exception("JSON syntax error in map set. Set must be of objects");
                var localMapper = mapper;
                for (var i = 0; i < argumentCount; i += 1)
                {
                    string value;
                    switch (obj.GetValue(keys[i], OrdinalIgnoreCase))
                    {
                        case JValue {Type: JTokenType.Null}:
                        case null:
                            value = "null";
                            break;
                        case JValue {Type: JTokenType.Date} jvalue:
                            value = jvalue.Value<DateTime>().ToString("O");
                            break;
                        case JValue jvalue when jvalue.Value<string>() is string stringValue:
                            value = stringValue == "" ? "\"\"" : stringValue;
                            break;
                        // exit inner loop, continue outer loop
                        default: goto next_item;
                    }
                    valueBuffer[i] = value.UriEncode();
                }
                localMapper = string.Format(localMapper, valueBuffer);
                await using (var innerRequest = request.Context.CreateRequest(localMapper))
                {
                    switch (await innerRequest.Evaluate())
                    {
                        case IEntities<object> entities:
                            mapped.UnionWith(entities.Select(e => e.ToJObject()).ToEnumerable());
                            break;
                        case var other:
                            throw new Exception($"Could not get source data from '{localMapper}'. {other.GetLogMessage().Result}");
                    }
                }
                next_item: ;
            }
            foreach (var item in mapper)
            {
                yield return item;
            }
        }

        private static IAsyncEnumerable<JToken>[] Checked(IAsyncEnumerable<JToken>[] arrays)
        {
            if (arrays == null || arrays.Length < 2 || arrays.Any(a => a == null))
                throw new ArgumentException(nameof(arrays));
            return arrays;
        }
    }
}