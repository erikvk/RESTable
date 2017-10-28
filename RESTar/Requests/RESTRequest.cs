using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ExcelDataReader;
using Newtonsoft.Json.Linq;
using RESTar.Internal;
using RESTar.Linq;
using RESTar.Operations;
using RESTar.Serialization;
using Starcounter;
using static System.Net.HttpStatusCode;
using static RESTar.Internal.ErrorCodes;
using static RESTar.RESTarConfig;
using static RESTar.Methods;
using IResource = RESTar.Internal.IResource;

namespace RESTar.Requests
{
    internal class RESTRequest<T> : IRequest<T>, IDisposable where T : class
    {
        public Methods Method { get; private set; }
        public Origin Origin { get; }
        public IResource<T> Resource { get; }
        public Condition<T>[] Conditions { get; private set; }
        public MetaConditions MetaConditions { get; private set; }
        public string Body { get; set; }
        public string AuthToken { get; internal set; }
        public IDictionary<string, string> ResponseHeaders { get; }
        IResource IRequest.Resource => Resource;
        internal Request ScRequest { get; }
        private Response Response { get; set; }
        private Func<RESTRequest<T>, Response> Evaluator { get; set; }
        private byte[] BinaryBody { get; set; }
        private string Source { get; set; }
        private string Destination { get; set; }
        private MimeType ContentType { get; set; }
        internal MimeType Accept { get; set; }
        private string CORSOrigin { get; set; }
        private DataConfig InputDataConfig { get; set; }
        private DataConfig OutputDataConfig { get; set; }
        internal void Evaluate() => Response = Evaluator(this);

        internal RESTRequest(IResource<T> resource, Request scRequest)
        {
            if (resource.IsInternal) throw new ResourceIsInternalException(resource);
            Resource = resource;
            ScRequest = scRequest;
            Origin = new Origin(scRequest);
            ResponseHeaders = new Dictionary<string, string>();
            Conditions = new Condition<T>[0];
            MetaConditions = new MetaConditions();
        }

        internal void Populate(Args args, Methods method)
        {
            Method = method;
            Evaluator = Evaluators<T>.REST.GetEvaluator(method);
            Source = ScRequest.Headers["Source"];
            Destination = ScRequest.Headers["Destination"];
            CORSOrigin = ScRequest.Headers["Origin"];
            ContentType = MimeTypes.Match(ScRequest.ContentType);
            Accept = MimeTypes.Match(ScRequest.PreferredMimeTypeString);
            InputDataConfig = Source != null ? DataConfig.External : DataConfig.Client;
            OutputDataConfig = Destination != null ? DataConfig.External : DataConfig.Client;
            if (args.HasConditions)
                Conditions = Condition<T>.Parse(args.Conditions, Resource) ?? Conditions;
            if (args.HasMetaConditions)
                MetaConditions = MetaConditions.Parse(args.MetaConditions, Resource) ?? MetaConditions;
        }

        internal void SetRequestData()
        {
            #region Resolve data source

            switch (InputDataConfig)
            {
                case DataConfig.Client:
                    if (ScRequest.Body == null && (Method == PATCH || Method == POST || Method == PUT))
                        throw new SyntaxException(NoDataSource, "Missing data source for method " + Method);
                    if (ScRequest.Body == null) return;
                    BinaryBody = ScRequest.BodyBytes;
                    Body = ScRequest.Body.Trim();
                    break;
                case DataConfig.External:
                    try
                    {
                        var request = new HttpRequest(Source)
                        {
                            Accept = ContentType.ToMimeString(),
                            AuthToken = AuthToken
                        };
                        if (request.Method != GET)
                            throw new SyntaxException(InvalidSource, "Only GET is allowed in Source headers");
                        var response = request.GetResponse() ?? throw new SourceException(request, "No response");
                        if (!response.IsSuccessStatusCode)
                            throw new SourceException(request,
                                $"Status: {response.StatusCode} - {response.StatusDescription}. {response.Headers["RESTar-info"]}");
                        if (response.BodyBytes?.Any() != true) throw new SourceException(request, "Response was empty");
                        BinaryBody = response.BodyBytes;
                        Body = response.Body.Trim();
                        break;
                    }
                    catch (HttpRequestException re)
                    {
                        throw new SyntaxException(InvalidSource, $"{re.Message} in the Source header");
                    }
            }

            #endregion

            #region Check data format

            switch (ContentType)
            {
                case MimeType.Json:
                    if (Body[0] == '[' && Method != POST)
                        throw new InvalidInputCountException(Method);
                    return;
                case MimeType.XML: throw new FormatException("XML is only supported as output format");
                case MimeType.Excel:
                    using (var stream = new MemoryStream(BinaryBody))
                    {
                        var regex = new Regex(@"(:[\d]+).0([\D])");
                        var reader = ExcelReaderFactory.CreateOpenXmlReader(stream);
                        var result = reader.AsDataSet() ?? throw new ExcelInputException();
                        if (Method == POST) Body = regex.Replace(result.Tables[0].Serialize(), "$1$2");
                        else
                        {
                            if (result.Tables[0].Rows.Count > 1) throw new InvalidInputCountException(Method);
                            Body = JArray.FromObject(result.Tables[0])[0].Serialize();
                        }
                    }
                    return;
            }

            #endregion
        }

        internal Response GetResponse()
        {
            ResponseHeaders.ForEach(h => Response.Headers["X-" + h.Key] = h.Value);
            Response.Headers["Access-Control-Allow-Origin"] = AllowAllOrigins ? "*" : (CORSOrigin ?? "null");
            switch (OutputDataConfig)
            {
                case DataConfig.Client: return Response;
                case DataConfig.External:
                    try
                    {
                        var request = new HttpRequest(Destination)
                        {
                            ContentType = Accept.ToMimeString(),
                            AuthToken = AuthToken,
                            Bytes = Response.BodyBytes
                        };
                        var response = request.GetResponse() ?? throw new DestinationException(request, "No response");
                        if (!response.IsSuccessStatusCode)
                            throw new DestinationException(request,
                                $"Received {response.StatusCode} - {response.StatusDescription}. {response.Headers["RESTar-info"]}");
                        response.Headers["Access-Control-Allow-Origin"] =
                            AllowAllOrigins ? "*" : (CORSOrigin ?? "null");
                        return response;
                    }
                    catch (HttpRequestException re)
                    {
                        throw new SyntaxException(InvalidDestination, $"{re.Message} in the Destination header");
                    }
                default: throw new ArgumentException();
            }
        }

        internal Response InsertedEntities(int count) => new Response
        {
            StatusCode = (ushort) Created,
            StatusDescription = "Created",
            Headers = {["RESTar-info"] = $"{count} entities inserted into resource '{Resource.Name}'"}
        };

        internal Response UpdatedEntities(int count) => new Response
        {
            StatusCode = (ushort) OK,
            StatusDescription = "OK",
            Headers = {["RESTar-info"] = $"{count} entities updated in resource '{Resource.Name}'"}
        };

        internal Response SafePostedEntities(int upd, int ins) => new Response
        {
            StatusCode = 200,
            StatusDescription = "OK",
            Headers =
            {
                ["RESTar-info"] = $"Updated {upd} and then inserted {ins} entities in resource '{Resource.Name}'"
            }
        };

        internal Response DeletedEntities(int count) => new Response
        {
            StatusCode = (ushort) OK,
            StatusDescription = "OK",
            Headers = {["RESTar-info"] = $"{count} entities deleted from resource '{Resource.Name}'"}
        };

        internal Response EntityCount(long Count) => new Response
        {
            StatusCode = (ushort) OK,
            StatusDescription = "OK",
            Headers = {["RESTar-info"] = $"Resource '{Resource.Name}'"},
            Body = new {Count}.Serialize()
        };

        public void Dispose()
        {
            if (ScRequest.IsExternal && AuthToken != null)
                AuthTokens.TryRemove(AuthToken, out var _);
        }
    }
}