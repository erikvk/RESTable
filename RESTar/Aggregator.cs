using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using RESTar.ContentTypeProviders;
using RESTar.Linq;
using RESTar.Requests;
using RESTar.Results.Error;
using RESTar.Results.Success;
using static Newtonsoft.Json.JsonToken;

namespace RESTar
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

    /// <inheritdoc cref="ISelector{T}" />
    /// <inheritdoc cref="Dictionary{TKey,TValue}" />
    /// <summary>
    /// A resource for creating arbitrary aggregated reports from multiple
    /// internal requests.
    /// </summary>
    [RESTar(Methods.GET, Description = description), JsonConverter(typeof(AggregatorTemplateConverter))]
    public class Aggregator : Dictionary<string, object>, ISelector<Aggregator>
    {
        private const string description = "A resource for creating arbitrary aggregated reports from multiple internal requests";

        /// <inheritdoc />
        public IEnumerable<Aggregator> Select(IRequest<Aggregator> request)
        {
            object populator(object node)
            {
                switch (node)
                {
                    case Aggregator obj:
                        obj.ToList().ForEach(pair => obj[pair.Key] = populator(pair.Value));
                        return obj;
                    case JArray array:
                        return array.Select(item => item.ToObject<object>()).Select(populator).ToList();
                    case JObject jobj:
                        return populator(jobj.ToObject<Aggregator>(JsonContentProvider.Serializer));

                    case string empty when string.IsNullOrWhiteSpace(empty): return empty;

                    case string stringValue:
                        Methods method;
                        string uri;
                        if (stringValue.StartsWith("GET "))
                        {
                            method = Methods.GET;
                            uri = stringValue.Substring(4);
                        }
                        else if (stringValue.StartsWith("REPORT "))
                        {
                            method = Methods.REPORT;
                            uri = stringValue.Substring(7);
                        }
                        else return stringValue;
                        if (string.IsNullOrWhiteSpace(uri))
                            throw new Exception($"Invalid URI in aggregator template. Expected relative uri after '{method.ToString()}'.");
                        switch (RequestEvaluator.Evaluate(request, method, ref uri, null, request.Headers).GetRawResult())
                        {
                            case RESTarError error: throw new Exception($"Could not get source data from '{uri}'. {error}");
                            case NoContent _: return null;
                            case Report report: return report.ReportBody.Count;
                            case Entities entities: return entities.Content;
                            case var other:
                                throw new Exception($"Unexpected result from {method.ToString()} query inside " +
                                                    $"Aggregator: {other.LogMessage}");
                        }
                    case var other: return other;
                }
            }

            if (!request.Body.HasContent)
                throw new Exception("Missing data source for Aggregator request");
            var _template = request.Body.ToList<Aggregator>().FirstOrDefault();
            populator(_template);
            return new[] {_template}.Where(request.Conditions);
        }
    }
}