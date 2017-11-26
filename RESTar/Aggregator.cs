using System;
using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json.Linq;
using RESTar.Http;
using RESTar.Linq;
using RESTar.Serialization;

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
            var template = request.BodyObject<JObject>();
            if (template == null) return null;

            void recursor(JToken token)
            {
                switch (token)
                {
                    case JProperty property:
                        var jvalue = property.Value;
                        if (jvalue.Type == JTokenType.Object)
                        {
                            recursor(jvalue);
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

                        var response = HttpRequest.Internal(method, new Uri(uriString, UriKind.Relative), request.AuthToken);
                        if (response?.IsSuccessStatusCode != true)
                            throw new Exception($"Could not get source data from '{uriString}'. {response?.StatusCode}: " +
                                                $"{response?.StatusDescription}. {response?.Headers["RESTar-info"]}");
                        switch (method)
                        {
                            case Methods.GET:
                                if (response.StatusCode == HttpStatusCode.NoContent || !(response.Body?.Length > 2))
                                    property.Value = new JArray();
                                else property.Value = response.Body.Deserialize<JArray>();
                                break;
                            case Methods.REPORT:
                                if (response.StatusCode == HttpStatusCode.NoContent || !(response.Body?.Length > 2))
                                    property.Value = 0;
                                else property.Value = response.Body.Deserialize<JObject>()["Count"];
                                break;
                        }
                        break;
                    case JObject obj:
                        obj.Properties().ForEach(recursor);
                        break;
                }
            }

            recursor(template);
            return new[] {new Aggregator(template)}.Where(request.Conditions);
        }
    }
}