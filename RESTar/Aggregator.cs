using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using RESTar.Http;
using RESTar.Linq;
using RESTar.Serialization;
using static System.Net.HttpStatusCode;

namespace RESTar
{
    /// <summary>
    /// A resource for creating arbitrary aggregated reports from multiple
    /// internal requests.
    /// </summary>
    [RESTar(Methods.GET, Description = description)]
    public class Aggregator : JObject, ISelector<Aggregator>
    {
        private const string description = "A resource for creating arbitrary aggregated reports from multiple internal requests";

        private Aggregator(JObject token) : base(token) { }

        /// <inheritdoc />
        public IEnumerable<Aggregator> Select(IRequest<Aggregator> request)
        {
            JObject getTemplate(JToken token)
            {
                switch (token)
                {
                    case null: return null;
                    case JObject obj: return obj;
                    case JArray arr when arr.Count == 1: return getTemplate(arr[0]);
                    default: throw new Exception("Invalid Aggregator template. Expected a single object");
                }
            }

            void populator(JToken token)
            {
                switch (token)
                {
                    case JProperty property:
                        var jvalue = property.Value;
                        if (jvalue.Type == JTokenType.Object)
                        {
                            populator(jvalue);
                            break;
                        }
                        if (jvalue.Type != JTokenType.String) break;
                        var stringValue = jvalue.Value<string>();
                        if (string.IsNullOrWhiteSpace(stringValue)) break;
                        Methods method = default;
                        string uriString;
                        var method_uri = stringValue.Split(' ');
                        if (method_uri.Length > 1)
                        {
                            uriString = method_uri[1];
                            switch (method_uri[0])
                            {
                                case "GET":
                                    method = Methods.GET;
                                    break;
                                case "REPORT":
                                    method = Methods.REPORT;
                                    break;
                                default:
                                    throw new Exception("Invalid method in template URI. " +
                                                        "Only GET and REPORT are allowed");
                            }
                        }
                        else uriString = method_uri[0];
                        if (uriString[0] != '/') break;

                        var response = HttpRequest.Internal(method, new Uri(uriString, UriKind.Relative), request.AuthToken);
                        if (response?.IsSuccessStatusCode != true)
                            throw new Exception(
                                $"Could not get source data from '{uriString}'. {response?.StatusCode.ToCode()}: " +
                                $"{response?.StatusDescription}. {response?.Headers?.SafeGet("RESTar-info")}");
                        switch (method)
                        {
                            case Methods.GET:
                                if (response.StatusCode == NoContent || !(response.Body?.Length > 2))
                                    property.Value = null;
                                else property.Value = response.Body.Deserialize<JToken>();
                                break;
                            case Methods.REPORT:
                                if (response.StatusCode == NoContent || !(response.Body?.Length > 2))
                                    property.Value = 0;
                                else property.Value = response.Body.Deserialize<JObject>()["Count"];
                                break;
                        }
                        break;
                    case JObject obj:
                        obj.Properties().ForEach(populator);
                        break;
                }
            }

            var tree = getTemplate(request.BodyObject<JToken>());
            populator(tree);
            return new[] {new Aggregator(tree)}.Where(request.Conditions);
        }
    }
}