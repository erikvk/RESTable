using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RESTable.Meta;
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
    public class SetOperations : Dictionary<string, object?>, IAsyncSelector<SetOperations>
    {
        private const string description = "The SetOperations resource can perform advanced operations " +
                                           "on entities in one or more RESTable resources. See the RESTable " +
                                           "Specification for details.";

        private static readonly IEqualityComparer<JsonElement> EqualityComparer = new JsonElementComparer();

        public SetOperations() { }
        private SetOperations(IDictionary<string, object?> other) : base(other) { }

        /// <inheritdoc />
        public async IAsyncEnumerable<SetOperations> SelectAsync(IRequest<SetOperations> request)
        {
            var inputElement = await request.Expecting
            (
                selector: async r => await r.Body.Deserialize<JsonElement>().FirstAsync().ConfigureAwait(false),
                errorMessage: "Expected expression tree as request body"
            ).ConfigureAwait(false);

            async IAsyncEnumerable<JsonElement> Recursor(JsonElement jsonElement)
            {
                switch (jsonElement)
                {
                    case {ValueKind: JsonValueKind.String} stringValue:
                        var argument = stringValue.GetString();
                        if (argument?.StartsWith("GET ") == true)
                            argument = argument.Substring(4);
                        switch (argument?.FirstOrDefault())
                        {
                            case null: throw new ArgumentException("Invalid operation expression. Expected string, found null");
                            case default(char): throw new ArgumentException("Operation expressions cannot be empty strings");
                            case '[':
                            {
                                var jsonArray = JsonSerializer.Deserialize<JsonElement>(argument);
                                foreach (var arrayItem in jsonArray.EnumerateArray())
                                    yield return arrayItem;
                                yield break;
                            }
                            case '/':
                            {
                                var innerRequest = request.Context.CreateRequest(uri: argument);
                                await using var result = await innerRequest.GetResult().ConfigureAwait(false);
                                switch (result)
                                {
                                    case IAsyncEnumerable<object> entities:
                                    {
                                        await foreach (var entity in entities.ConfigureAwait(false))
                                        {
                                            yield return entity.ToJsonElement();
                                        }
                                        yield break;
                                    }
                                    case var other:
                                    {
                                        var logMessage = await other.GetLogMessage().ConfigureAwait(false);
                                        throw new ArgumentException($"Could not get source data from '{argument}'. {logMessage}");
                                    }
                                }
                            }
                            default:
                            {
                                throw new ArgumentException($"Invalid string '{argument}'. Must be a relative REST request URI " +
                                                            "beginning with '/<resource locator>' or a JSON array.");
                            }
                        }
                    case {ValueKind: JsonValueKind.Object} objectValue when
                        objectValue.EnumerateObject().ToList() is {Count : 1} valueList &&
                        valueList.First() is {Value: {ValueKind: JsonValueKind.Array} arrayValue} arrayProperty:
                    {
                        var arrayLength = arrayValue.GetArrayLength();
                        switch (arrayProperty.Name.ToLower())
                        {
                            case "distinct":
                            {
                                if (arrayLength != 1)
                                    throw new ArgumentException("Distinct takes one and only one set as operand");
                                await foreach (var item in Distinct(Recursor(arrayValue[0])).ConfigureAwait(false))
                                    yield return item;
                                yield break;
                            }
                            case "except":
                                if (arrayLength != 2)
                                    throw new ArgumentException("Except takes two and only two argument sets as operands");
                                await foreach (var item in Except(Recursor(arrayValue[0]), Recursor(arrayValue[1])).ConfigureAwait(false))
                                    yield return item;
                                yield break;
                            case "intersect":
                            {
                                if (arrayLength < 2)
                                    throw new ArgumentException("Intersect takes at least two sets as operands");

                                var tokens = new IAsyncEnumerable<JsonElement>[arrayLength];
                                for (var i = 0; i < tokens.Length; i += 1)
                                    tokens[i] = Recursor(arrayValue[i]);
                                await foreach (var item in Intersect(tokens).ConfigureAwait(false))
                                    yield return item;
                                yield break;
                            }
                            case "union":
                            {
                                if (arrayLength < 2)
                                    throw new ArgumentException("Union takes at least two sets as operands");
                                var tokens = new IAsyncEnumerable<JsonElement>[arrayLength];
                                for (var i = 0; i < tokens.Length; i += 1)
                                    tokens[i] = Recursor(arrayValue[i]);
                                await foreach (var item in Union(tokens).ConfigureAwait(false))
                                    yield return item;
                                yield break;
                            }
                            case "map":
                                if (arrayLength != 2)
                                    throw new ArgumentException("Map takes two and only two arguments");
                                var mapper = arrayValue[1].GetString() ?? throw new ArgumentException("Missing mapper");
                                await foreach (var item in Map(Recursor(arrayValue[0]), mapper, request).ConfigureAwait(false))
                                    yield return item;
                                yield break;
                            default:
                                throw new ArgumentException(
                                    $"Unknown operation '{arrayProperty.Name}'. Avaliable operations: distinct, except, " +
                                    "intersect, union.");
                        }
                    }
                    default:
                        throw new ArgumentException(
                            $"Invalid type '{jsonElement.ValueKind}' in operations tree. Expected object with single array " +
                            "property, or string");
                }
            }

            await foreach (var element in Recursor(inputElement).ConfigureAwait(false))
            {
                var populated = element switch
                {
                    {ValueKind: JsonValueKind.Object} objectElement => objectElement.ToObject<SetOperations>(),
                    var value => new SetOperations {["Value"] = value}
                };
                yield return populated!;
            }
        }

        private static IAsyncEnumerable<JsonElement>? Distinct(IAsyncEnumerable<JsonElement>? array)
        {
            return array?.Distinct(EqualityComparer);
        }

        private static IAsyncEnumerable<JsonElement> Intersect(params IAsyncEnumerable<JsonElement>[] arrays)
        {
            return Checked(arrays).Aggregate((x, y) => x.Intersect(y, EqualityComparer));
        }

        private static IAsyncEnumerable<JsonElement> Union(params IAsyncEnumerable<JsonElement>[] arrays)
        {
            return Checked(arrays).Aggregate((x, y) => x.Union(y, EqualityComparer));
        }

        private static IAsyncEnumerable<JsonElement> Except(params IAsyncEnumerable<JsonElement>[] arrays)
        {
            return Checked(arrays).Aggregate((x, y) => x.Except(y, EqualityComparer));
        }

        private static async IAsyncEnumerable<JsonElement> Map(IAsyncEnumerable<JsonElement> set, string mapper, IRequest request)
        {
            if (set is null) throw new ArgumentException(nameof(set));
            if (string.IsNullOrEmpty(mapper)) throw new ArgumentException(nameof(mapper));

            var mapped = new HashSet<JsonElement>(EqualityComparer);
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
                if (item.ValueKind != JsonValueKind.Object)
                    throw new Exception("JSON syntax error in map set. Set must be of objects");
                var localMapper = mapper;
                var skip = false;
                for (var i = 0; i < argumentCount; i += 1)
                {
                    string? value;
                    switch (item.GetProperty(keys[i], OrdinalIgnoreCase)?.Value)
                    {
                        case null:
                        case {ValueKind: JsonValueKind.Null}:
                            value = "null";
                            break;
                        case {ValueKind: JsonValueKind.String} dateValue when DateTime.TryParse(dateValue.GetString()!, out var dateTime):
                            value = dateTime.ToString("O");
                            break;
                        case {ValueKind: JsonValueKind.String} stringElement when stringElement.GetString() is string stringValue:
                            value = stringValue == "" ? "\"\"" : stringValue;
                            break;
                        default:
                            value = null;
                            skip = true;
                            break;
                    }
                    if (skip)
                        break;
                    valueBuffer[i] = value.UriEncode()!;
                }
                if (skip) continue;
                localMapper = string.Format(localMapper, valueBuffer);
                var innerRequest = request.Context.CreateRequest(uri: localMapper);
                await using var result = await innerRequest.GetResult().ConfigureAwait(false);
                if (result is IEntities<object> entities)
                {
                    await foreach (var entity in entities)
                    {
                        var jobject = entity.ToJsonElement();
                        mapped.Add(jobject);
                    }
                }
                else throw new Exception($"Could not get source data from '{localMapper}'. {await result.GetLogMessage().ConfigureAwait(false)}");
            }
            foreach (var item in mapped)
            {
                yield return item;
            }
        }

        private static IAsyncEnumerable<JsonElement>[] Checked(IAsyncEnumerable<JsonElement>?[] arrays)
        {
            if (arrays is null || arrays.Length < 2 || arrays.Any(a => a is null))
                throw new ArgumentException(nameof(arrays));
            return arrays!;
        }
    }
}