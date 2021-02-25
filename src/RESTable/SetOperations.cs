using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
            var jobject = await request.Expecting
            (
                selector: async r => await r.Body.Deserialize<JObject>().FirstAsync().ConfigureAwait(false),
                errorMessage: "Expected expression tree as request body"
            ).ConfigureAwait(false);

            var jsonSerializer = request.GetService<JsonSerializer>();

            async IAsyncEnumerable<JToken> Recursor(JToken token)
            {
                switch (token)
                {
                    case JValue {Type: JTokenType.String} stringValue:
                        var argument = stringValue.Value<string>();
                        if (argument?.StartsWith("GET ") == true)
                            argument = argument.Substring(4);
                        switch (argument?.FirstOrDefault())
                        {
                            case null: throw new ArgumentException("Invalid operation expression. Expected string, found null");
                            case default(char): throw new ArgumentException("Operation expressions cannot be empty strings");
                            case '[':
                            {
                                foreach (var arrayItem in JArray.Parse(argument))
                                    yield return arrayItem;
                                yield break;
                            }
                            case '/':
                            {
                                await using var innerRequest = request.Context.CreateRequest(uri: argument);
                                var result = await innerRequest.Evaluate().ConfigureAwait(false);
                                switch (result)
                                {
                                    case IEntities entities:
                                    {
                                        foreach (var item in JArray.FromObject(entities, jsonSerializer))
                                            yield return item;
                                        yield break;
                                    }
                                    case var other: throw new ArgumentException($"Could not get source data from '{argument}'. {other.GetLogMessage().Result}");
                                }
                            }
                            default:
                                throw new ArgumentException($"Invalid string '{argument}'. Must be a relative REST request URI " +
                                                            "beginning with '/<resource locator>' or a JSON array.");
                        }
                    case JObject {Count: 1, First: JProperty {Value: JArray arr} prop}:
                        switch (prop.Name.ToLower())
                        {
                            case "distinct":
                                if (arr.Count != 1)
                                    throw new ArgumentException("Distinct takes one and only one set as operand");
                                await foreach (var item in Distinct(Recursor(arr.First)).ConfigureAwait(false))
                                    yield return item;
                                yield break;
                            case "except":
                                if (arr.Count != 2)
                                    throw new ArgumentException("Except takes two and only two argument sets as operands");
                                await foreach (var item in Except(Recursor(arr[0]), Recursor(arr[1])).ConfigureAwait(false))
                                    yield return item;
                                yield break;
                            case "intersect":
                            {
                                if (arr.Count < 2)
                                    throw new ArgumentException("Intersect takes at least two sets as operands");

                                var tokens = new IAsyncEnumerable<JToken>[arr.Count];
                                for (var i = 0; i < tokens.Length; i += 1)
                                    tokens[i] = Recursor(arr[i]);
                                await foreach (var item in Intersect(tokens).ConfigureAwait(false))
                                    yield return item;
                                yield break;
                            }
                            case "union":
                            {
                                if (arr.Count < 2)
                                    throw new ArgumentException("Union takes at least two sets as operands");
                                var tokens = new IAsyncEnumerable<JToken>[arr.Count];
                                for (var i = 0; i < tokens.Length; i += 1)
                                    tokens[i] = Recursor(arr[i]);
                                await foreach (var item in Union(tokens).ConfigureAwait(false))
                                    yield return item;
                                yield break;
                            }
                            case "map":
                                if (arr.Count != 2)
                                    throw new ArgumentException("Map takes two and only two arguments");
                                await foreach (var item in Map(Recursor(arr[0]), (string) arr[1], request).ConfigureAwait(false))
                                    yield return item;
                                yield break;
                            default:
                                throw new ArgumentException(
                                    $"Unknown operation '{prop.Name}'. Avaliable operations: distinct, except, " +
                                    "intersect, union.");
                        }
                    default:
                        throw new ArgumentException(
                            $"Invalid type '{token.Type}' in operations tree. Expected object with single array " +
                            "property, or string");
                }
            }

            await foreach (var token in Recursor(jobject).ConfigureAwait(false))
            {
                yield return token switch
                {
                    JValue value => new SetOperations(new JObject(new JProperty("Value", value))),
                    JObject @object => new SetOperations(@object),
                    _ => throw new Exception("Invalid entity type in set operation")
                };
            }
        }

        private static IAsyncEnumerable<JToken> Distinct(IAsyncEnumerable<JToken> array)
        {
            return array?.Distinct(EqualityComparer);
        }

        private static IAsyncEnumerable<JToken> Intersect(params IAsyncEnumerable<JToken>[] arrays)
        {
            return Checked(arrays).Aggregate((x, y) => x.Intersect(y, EqualityComparer));
        }

        private static IAsyncEnumerable<JToken> Union(params IAsyncEnumerable<JToken>[] arrays)
        {
            return Checked(arrays).Aggregate((x, y) => x.Union(y, EqualityComparer));
        }

        private static IAsyncEnumerable<JToken> Except(params IAsyncEnumerable<JToken>[] arrays)
        {
            return Checked(arrays).Aggregate((x, y) => x.Except(y, EqualityComparer));
        }

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

            await foreach (var item in Distinct(set).ConfigureAwait(false))
            {
                var obj = item as JObject ?? throw new Exception("JSON syntax error in map set. Set must be of objects");
                var localMapper = mapper;
                var skip = false;
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
                        default:
                            value = null;
                            skip = true;
                            break;
                    }
                    if (skip) break;
                    valueBuffer[i] = value.UriEncode();
                }
                if (skip) continue;
                localMapper = string.Format(localMapper, valueBuffer);
                await using var innerRequest = request.Context.CreateRequest(uri: localMapper);
                var result = await innerRequest.Evaluate().ConfigureAwait(false);
                if (result is IEntities<object> entities)
                {
                    await foreach (var entity in entities.ConfigureAwait(false)) 
                        mapped.Add(entity.ToJObject());
                }
                else throw new Exception($"Could not get source data from '{localMapper}'. {await result.GetLogMessage().ConfigureAwait(false)}");
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