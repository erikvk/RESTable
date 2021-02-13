using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using RESTable.ContentTypeProviders;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Resources.Operations;
using RESTable.Results;
using static System.StringComparison;
using static Newtonsoft.Json.JsonToken;

namespace RESTable
{
    internal class AggregatorTemplateConverter : CustomCreationConverter<Aggregator>
    {
        public override Aggregator Create(Type objectType) => new Aggregator();

        public override bool CanConvert(Type objectType) => objectType == typeof(object) || base.CanConvert(objectType);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            switch (reader.TokenType)
            {
                case Null:
                case StartObject:
                    return base.ReadJson(reader, objectType, existingValue, serializer);
                default: return serializer.Deserialize(reader);
            }
        }
    }

    /// <inheritdoc cref="RESTable.Resources.Operations.ISelector{T}" />
    /// <inheritdoc cref="Dictionary{TKey,TValue}" />
    /// <summary>
    /// A resource for creating arbitrary aggregated reports from multiple
    /// internal requests.
    /// </summary>
    [RESTable(Method.GET, Description = description), JsonConverter(typeof(AggregatorTemplateConverter))]
    public class Aggregator : Dictionary<string, object>, IAsyncSelector<Aggregator>
    {
        private const string description = "A resource for creating arbitrary aggregated reports from multiple internal requests";

        /// <inheritdoc />
        public async IAsyncEnumerable<Aggregator> SelectAsync(IRequest<Aggregator> request)
        {
            async Task<object> Populator(object node)
            {
                switch (node)
                {
                    case Aggregator aggregator:
                        foreach (var (key, obj) in aggregator.ToList())
                        {
                            var value = await Populator(obj);
                            switch (key)
                            {
                                case "$add" when IsNumberArray(value, out var terms): return terms.Sum();
                                case "$sub" when IsNumberArray(value, out var terms): return terms.Aggregate((x, y) => x - y);
                                case "$mul" when IsNumberArray(value, out var terms): return terms.Aggregate((x, y) => x * y);
                                case "$mod" when IsNumberArray(value, out var terms, 2): return terms[0] % terms[1];

                                case "$add": throw GetArithmeticException(key);
                                case "$sub": throw GetArithmeticException(key);
                                case "$mul": throw GetArithmeticException(key);
                                case "$mod": throw GetArithmeticException(key, "For $mod, the integer list must have a length of exacly 2");
                            }
                            aggregator[key] = value;
                        }
                        return aggregator;
                    case JArray array:
                    {
                        var list = new List<object>();
                        foreach (var item in array)
                        {
                            var obj = item.ToObject<object>();
                            var populated = await Populator(obj);
                            list.Add(populated);
                        }
                        return list;
                    }
                    case JObject jobj: return await Populator(jobj.ToObject<Aggregator>(JsonProvider.Serializer));
                    case string empty when string.IsNullOrWhiteSpace(empty): return empty;
                    case string stringValue:
                    {
                        Method method;
                        string uri;
                        if (stringValue.StartsWith("GET ", OrdinalIgnoreCase))
                        {
                            method = Method.GET;
                            uri = stringValue.Substring(4);
                        }
                        else if (stringValue.StartsWith("REPORT ", OrdinalIgnoreCase))
                        {
                            method = Method.REPORT;
                            uri = stringValue.Substring(7);
                        }
                        else return stringValue;
                        if (string.IsNullOrWhiteSpace(uri))
                            throw new Exception($"Invalid URI in aggregator template. Expected relative uri after '{method.ToString()}'.");
                        await using var internalRequest = request.Context.CreateRequest
                        (
                            uri: uri,
                            method: method,
                            headers: request.Headers
                        );
                        var result = await internalRequest.Evaluate();
                        switch (result)
                        {
                            case Error error: throw new Exception($"Could not get source data from '{uri}'. The resource returned: {error}");
                            case Report report: return report.ReportBody.Count;
                            case IEntities entities: return entities;
                            case var other:
                                throw new Exception($"Unexpected result from {method.ToString()} request inside " +
                                                    $"Aggregator: {other.GetLogMessage().Result}");
                        }
                    }
                    case var other: return other;
                }
            }

            var template = await request.Expecting
            (
                selector: async r => await r.Body.Deserialize<Aggregator>().FirstAsync(),
                message: "Expected an aggregator template as request body"
            );
            yield return await Populator(template) switch
            {
                Aggregator aggregator => aggregator,
                long integer => new Aggregator {["Result"] = integer},
                var other => throw new InvalidOperationException($"An error occured when reading the request template, the root object was resolved to {other.GetType().FullName}")
            };
        }

        private static Exception GetArithmeticException(string operation, string message = null)
        {
            return new InvalidOperationException($"Invalid arguments for operation '{operation}'. The value for key '{operation}' must evaluate " +
                                                 $"to a list of integers. {message}");
        }

        private static bool IsNumberArray(object _value, out long[] array, int? ofLength = null)
        {
            try
            {
                array = ((IList) _value).Cast<long>().ToArray();
                if (ofLength.HasValue) return array.Length == ofLength.Value;
                return true;
            }
            catch (InvalidCastException)
            {
                array = null;
                return false;
            }
        }
    }
}