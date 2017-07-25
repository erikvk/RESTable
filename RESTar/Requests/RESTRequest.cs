using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Excel;
using Newtonsoft.Json.Linq;
using RESTar.Internal;
using RESTar.Linq;
using RESTar.Operations;
using Starcounter;
using static RESTar.Internal.ErrorCodes;
using static RESTar.Operations.Do;
using static RESTar.RESTarConfig;
using static RESTar.RESTarMethods;
using IResource = RESTar.Internal.IResource;

namespace RESTar.Requests
{
    internal class RESTRequest<T> : IRequest<T>, IDisposable where T : class
    {
        public RESTarMethods Method { get; private set; }
        public IResource<T> Resource { get; }
        public Condition<T>[] Conditions { get; private set; }
        public MetaConditions MetaConditions { get; private set; }
        public string Body { get; set; }
        public string AuthToken { get; internal set; }
        public IDictionary<string, string> ResponseHeaders { get; }
        IResource IRequest.Resource => Resource;
        internal Request ScRequest { get; }
        private Response Response { get; set; }
        private RESTEvaluator<T> Evaluator { get; set; }
        private byte[] BinaryBody { get; set; }
        private string Source { get; set; }
        private string Destination { get; set; }
        private RESTarMimeType ContentType { get; set; }
        private RESTarMimeType Accept { get; set; }
        private string Origin { get; set; }
        private DataConfig InputDataConfig { get; set; }
        private DataConfig OutputDataConfig { get; set; }
        internal void Evaluate() => Response = Evaluator(this);

        internal RESTRequest(IResource<T> resource, Request scRequest)
        {
            if (resource.IsInternal) throw new ResourceIsInternalException(resource);
            Resource = resource;
            ScRequest = scRequest;
            ResponseHeaders = new Dictionary<string, string>();
            Conditions = new Condition<T>[0];
            MetaConditions = new MetaConditions();
        }

        internal void Populate(Args args, RESTarMethods method)
        {
            Method = method;
            Evaluator = Evaluators<T>.REST.GetEvaluator(method);
            Source = ScRequest.Headers["Source"];
            Destination = ScRequest.Headers["Destination"];
            Origin = ScRequest.Headers["Origin"];
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
                    Body = ScRequest.Body;
                    break;
                case DataConfig.External:
                    var rqst = TryCatch(
                        @try: () => HttpRequest.Parse(Source),
                        @catch: e => throw new SyntaxException(InvalidSource, $"{e.Message} in the source header")
                    );
                    if (rqst.Method != GET)
                        throw new SyntaxException(InvalidSource, "Only GET is allowed in Source headers");
                    rqst.Accept = ContentType.ToMimeString();
                    var response = rqst.Internal
                        ? HTTP.InternalRequest(GET, rqst.URI, AuthToken, headers: rqst.Headers, accept: rqst.Accept)
                        : HTTP.ExternalRequest(GET, rqst.URI, headers: rqst.Headers, accept: rqst.Accept);
                    if (response == null) throw new SourceException(rqst.URI, "No response");
                    if (!response.IsSuccessStatusCode)
                        throw new SourceException(rqst.URI,
                            $"Status: {response.StatusCode} - {response.StatusDescription}. {response.Headers["RESTar-info"]}");
                    if (response.BodyBytes?.Any() != true) throw new SourceException(rqst.URI, "Response was empty");
                    BinaryBody = response.BodyBytes;
                    Body = response.Body;
                    break;
            }

            #endregion

            #region Check data format

            switch (ContentType)
            {
                case RESTarMimeType.Json:
                    if (Body[0] == '[' && Method != POST)
                        throw new InvalidInputCountException(Method);
                    return;
                case RESTarMimeType.XML: throw new FormatException("XML is only supported as output format");
                case RESTarMimeType.Excel:
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

        internal Response MakeResponse(IEnumerable<object> data)
        {
            var fileName = $"{Resource.AliasOrName}_{DateTime.Now:yyyyMMdd_HHmmss}";
            switch (Accept)
            {
                case RESTarMimeType.Json:
                    var json = data.Serialize();
                    if (json == "[]") return null;
                    return new Response
                    {
                        ContentType = MimeTypes.JSON,
                        Body = json,
                        Headers = {["Content-Disposition"] = $"attachment; filename={fileName}.json"}
                    };
                case RESTarMimeType.Excel:
                    var excel = data.ToExcel(Resource)?.SerializeExcel();
                    if (excel == null) return null;
                    return new Response
                    {
                        ContentType = MimeTypes.Excel,
                        BodyBytes = excel,
                        Headers = {["Content-Disposition"] = $"attachment; filename={fileName}.xlsx"}
                    };
                case RESTarMimeType.XML:
                    var xml = data.SerializeXML();
                    if (xml == null) return null;
                    return new Response
                    {
                        ContentType = MimeTypes.XML,
                        Body = xml,
                        Headers = {["Content-Disposition"] = $"attachment; filename={fileName}.xml"}
                    };
                default: throw new ArgumentOutOfRangeException(nameof(Accept));
            }
        }


        internal Response GetResponse()
        {
            ResponseHeaders.ForEach(h => Response.Headers["X-" + h.Key] = h.Value);
            Response.Headers["Access-Control-Allow-Origin"] = AllowAllOrigins ? "*" : (Origin ?? "null");
            switch (OutputDataConfig)
            {
                case DataConfig.Client: return Response;
                case DataConfig.External:
                    var rqst = TryCatch(
                        @try: () => HttpRequest.Parse(Destination),
                        @catch: e => throw new SyntaxException(InvalidDestination,
                            $"{e.Message} in the destination header")
                    );
                    rqst.ContentType = Accept.ToMimeString();
                    var bytes = Response.BodyBytes;
                    var response = rqst.Internal
                        ? HTTP.InternalRequest(rqst.Method, rqst.URI, AuthToken, bytes, rqst.ContentType,
                            headers: rqst.Headers)
                        : HTTP.ExternalRequest(rqst.Method, rqst.URI, bytes, rqst.ContentType, headers: rqst.Headers);
                    if (response == null) throw new DestinationException(rqst.URI, "No response");
                    if (!response.IsSuccessStatusCode)
                        throw new DestinationException(rqst.URI,
                            $"Status: {response.StatusCode} - {response.StatusDescription}. {response.Headers["RESTar-info"]}");
                    response.Headers["Access-Control-Allow-Origin"] = AllowAllOrigins ? "*" : (Origin ?? "null");
                    return response;
                default: throw new ArgumentException();
            }
        }

        public void Dispose() => AuthTokens.TryRemove(AuthToken, out var _);
    }
}