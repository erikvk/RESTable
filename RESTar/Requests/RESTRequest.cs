using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using Excel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RESTar.Internal;
using RESTar.Operations;
using Starcounter;
using static RESTar.Internal.ErrorCodes;
using static RESTar.RESTarConfig;
using static RESTar.RESTarMethods;
using IResource = RESTar.Internal.IResource;

namespace RESTar.Requests
{
    internal class RESTRequest<T> : IRequest<T>, IDisposable where T : class
    {
        public RESTarMethods Method { get; private set; }
        public IResource<T> Resource { get; }
        public Conditions Conditions { get; private set; }
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
            Resource = resource;
            ScRequest = scRequest;
            ResponseHeaders = new Dictionary<string, string>();
            MetaConditions = new MetaConditions();
        }

        internal void Populate(string[] args, RESTarMethods method)
        {
            Method = method;
            Evaluator = Evaluators<T>.REST.GetEvaluator(method);
            Source = ScRequest.Headers["Source"];
            Destination = ScRequest.Headers["Destination"];
            Origin = ScRequest.Headers["Origin"];
            ContentType = MimeTypes.Match(ScRequest.ContentType);
            Accept = MimeTypes.Match(ScRequest.PreferredMimeTypeString);
            InputDataConfig = Source != null ? DataConfig.External : DataConfig.Internal;
            OutputDataConfig = Destination != null ? DataConfig.External : DataConfig.Internal;
            if (args.Length <= 2) return;
            Conditions = Conditions.Parse(args[2], Resource);
            if (args.Length == 3) return;
            MetaConditions = MetaConditions.Parse(args[3], Resource) ?? MetaConditions;
        }

        internal void SetRequestData()
        {
            #region Resolve data source

            switch (InputDataConfig)
            {
                case DataConfig.Internal:
                    if (ScRequest.Body == null && (Method == PATCH || Method == POST || Method == PUT))
                        throw new SyntaxException(NoDataSourceError, "Missing data source for method " + Method);
                    if (ScRequest.Body == null) return;
                    BinaryBody = ScRequest.BodyBytes;
                    Body = ScRequest.Body;
                    break;
                case DataConfig.External:
                    var rqst = HttpRequest.Parse(Source);
                    if (rqst.Method != GET)
                        throw new SyntaxException(InvalidSourceFormatError, "Only GET is allowed in Source headers");
                    rqst.Accept = ContentType.ToMimeString();
                    var response = rqst.Internal
                        ? HTTP.InternalRequest(GET, rqst.URI, AuthToken, headers: rqst.Headers, accept: rqst.Accept)
                        : HTTP.ExternalRequest(GET, rqst.URI, headers: rqst.Headers, accept: rqst.Accept);
                    if (response?.IsSuccessStatusCode != true)
                        throw new SourceException(Source, $"{response?.StatusCode}: {response?.StatusDescription}");
                    if (response.BodyBytes.IsNullOrEmpty()) throw new SourceException(Source, "Response was empty");
                    BinaryBody = response.BodyBytes;
                    Body = response.Body;
                    break;
            }

            #endregion

            #region Check data format

            switch (ContentType)
            {
                case RESTarMimeType.Json:
                    if (Body.First() == '[' && Method != POST)
                        throw new InvalidInputCountException(Method);
                    return;
                case RESTarMimeType.XML: throw new FormatException("XML is only supported as output format");
                case RESTarMimeType.Excel:
                    using (var stream = new MemoryStream(BinaryBody))
                    {
                        var regex = new Regex(@"(:[\d]+).0([\D])");
                        var excelReader = ExcelReaderFactory.CreateOpenXmlReader(stream);
                        excelReader.IsFirstRowAsColumnNames = true;
                        var result = excelReader.AsDataSet() ?? throw new ExcelInputException();
                        if (Method == POST) Body = regex.Replace(result.Tables[0].Serialize(), "$1$2");
                        else
                        {
                            if (result.Tables[0].Rows.Count > 1) throw new InvalidInputCountException(Method);
                            Body = JArray.FromObject(result.Tables[0]).First().Serialize();
                        }
                    }
                    return;
            }

            #endregion
        }

        internal void SetResponseData(IEnumerable<dynamic> data, Response response)
        {
            switch (Accept)
            {
                case RESTarMimeType.Json:
                    response.Body = data.Serialize();
                    response.ContentType = MimeTypes.JSON;
                    return;
                case RESTarMimeType.Excel:
                    var fileName = $"{Resource.Name}_output_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                    response.BodyBytes = data.ToExcel(Resource).SerializeExcel();
                    response.Headers["Content-Disposition"] = $"attachment; filename={fileName}";
                    response.ContentType = MimeTypes.Excel;
                    return;
                case RESTarMimeType.XML:
                    var json = data.Serialize();
                    var xml = JsonConvert.DeserializeXmlNode($@"{{""row"":{json}}}", "root", true);
                    using (var stringWriter = new StringWriter())
                    using (var xmlTextWriter = XmlWriter.Create(stringWriter))
                    {
                        xml.WriteTo(xmlTextWriter);
                        xmlTextWriter.Flush();
                        response.Body = stringWriter.GetStringBuilder().ToString();
                    }
                    response.ContentType = MimeTypes.XML;
                    return;
                default: throw new ArgumentOutOfRangeException(nameof(Accept));
            }
        }

        internal Response GetResponse()
        {
            ResponseHeaders.ForEach(h => Response.Headers["X-" + h.Key] = h.Value);
            Response.Headers["Access-Control-Allow-Origin"] = AllowAllOrigins ? "*" : (Origin ?? "null");
            if (OutputDataConfig == DataConfig.Internal)
                return Response;
            var rqst = HttpRequest.Parse(Destination);
            rqst.ContentType = Accept.ToMimeString();
            var bytes = Response.BodyBytes;
            var response = rqst.Internal
                ? HTTP.InternalRequest(rqst.Method, rqst.URI, AuthToken, bytes, rqst.ContentType, headers: rqst.Headers)
                : HTTP.ExternalRequest(rqst.Method, rqst.URI, bytes, rqst.ContentType, headers: rqst.Headers);
            if (response == null) throw new Exception($"No response for destination request: '{Destination}'");
            if (!response.IsSuccessStatusCode)
                throw new Exception($"Failed upload at destination server at '{rqst.URI}'. " +
                                    $"Status: {response.StatusCode}, {response.StatusDescription}");
            response.Headers["Access-Control-Allow-Origin"] = AllowAllOrigins ? "*" : (Origin ?? "null");
            return response;
        }

        public void Dispose()
        {
            AuthTokens.TryRemove(AuthToken, out var _);
        }
    }
}