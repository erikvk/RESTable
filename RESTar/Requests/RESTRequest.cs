using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using Excel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RESTar.Auth;
using RESTar.Internal;
using RESTar.Operations;
using Starcounter;
using static RESTar.Internal.ErrorCodes;
using static RESTar.RESTarConfig;
using static RESTar.RESTarMethods;

namespace RESTar.Requests
{
    internal class RESTRequest<T> : IRequest<T> where T : class
    {
        public RESTarMethods Method { get; private set; }
        public IResource<T> Resource { get; }
        public Conditions Conditions { get; private set; }
        public MetaConditions MetaConditions { get; private set; }
        public string Body { get; private set; }
        public string AuthToken { get; internal set; }
        public bool Internal => !ScRequest.IsExternal;
        public IDictionary<string, string> ResponseHeaders { get; }
        IResourceView IRequestView.Resource => Resource;

        internal Request ScRequest { get; }
        private Response Response { get; set; }
        private RESTEvaluator<T> Evaluator { get; set; }
        internal void Evaluate() => Response = Evaluator(this);
        private byte[] BinaryBody { get; set; }
        private string Source { get; set; }
        private string Destination { get; set; }
        private RESTarMimeType ContentType { get; set; }
        private RESTarMimeType Accept { get; set; }
        private string Origin { get; set; }
        private bool SerializeDynamic => MetaConditions.Dynamic || Resource.IsDynamic || MetaConditions.HasProcessors;

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
            if (args.Length <= 2) return;
            Conditions = Conditions.Parse(args[2], Resource);
            if (args.Length == 3) return;
            MetaConditions = MetaConditions.Parse(args[3], Resource) ?? MetaConditions;
        }

        internal void GetRequestData()
        {
            if (Source != null)
            {
                var sourceRequest = HttpRequest.Parse(Source);
                if (sourceRequest.Method != GET)
                    throw new SyntaxException(InvalidSourceFormatError, "Only GET is allowed in Source headers");
                sourceRequest.Accept = ContentType.ToMimeString();

                var response = sourceRequest.Internal
                    ? HTTP.InternalRequest
                    (
                        method: GET,
                        relativeUri: sourceRequest.URI,
                        authToken: AuthToken,
                        headers: sourceRequest.Headers,
                        accept: sourceRequest.Accept
                    )
                    : HTTP.ExternalRequest
                    (
                        method: GET,
                        uri: sourceRequest.URI,
                        headers: sourceRequest.Headers,
                        accept: sourceRequest.Accept
                    );

                if (response?.IsSuccessStatusCode != true)
                    throw new SourceException(Source, $"{response?.StatusCode}: {response?.StatusDescription}");

                if (ContentType == RESTarMimeType.Excel)
                {
                    BinaryBody = response.BodyBytes;
                    if (BinaryBody?.Any() != true)
                        throw new SourceException(Source, "Response was empty");
                }
                else
                {
                    Body = response.Body?.RemoveTabsAndBreaks();
                    if (Body == null)
                        throw new SourceException(Source, "Response was empty");
                    return;
                }
            }
            else
            {
                if (ScRequest.Body == null && (Method == PATCH || Method == POST || Method == PUT))
                    throw new SyntaxException(NoDataSourceError, "Missing data source for method " + Method);
                if (ScRequest.Body == null)
                    return;
            }

            switch (ContentType)
            {
                case RESTarMimeType.Json:
                    Body = Body?.Trim() ?? ScRequest.Body.Trim();
                    if (Body?.First() == '[' && Method != POST)
                        throw new InvalidInputCountException(Method);
                    break;
                case RESTarMimeType.Excel:
                    using (var stream = new MemoryStream(BinaryBody ?? ScRequest.BodyBytes))
                    {
                        var regex = new Regex(@"(:[\d]+).0([\D])");
                        var excelReader = ExcelReaderFactory.CreateOpenXmlReader(stream);
                        excelReader.IsFirstRowAsColumnNames = true;
                        var result = excelReader.AsDataSet();
                        if (result == null)
                            throw new ExcelInputException();
                        if (Method == POST)
                        {
                            Body = result.Tables[0].Serialize();
                            Body = regex.Replace(Body, "$1$2");
                        }
                        else
                        {
                            if (result.Tables[0].Rows.Count > 1)
                                throw new InvalidInputCountException(Method);
                            Body = JArray.FromObject(result.Tables[0]).First().Serialize();
                        }
                    }
                    break;
                case RESTarMimeType.XML: throw new FormatException("XML is only supported as output format");
            }
        }

        internal void SetResponseData(IEnumerable<dynamic> data, Response response)
        {
            switch (Accept)
            {
                case RESTarMimeType.Json:
                    response.Body = SerializeDynamic ? data.Serialize() : data.Serialize(IEnumTypes[Resource]);
                    response.ContentType = MimeTypes.JSON;
                    return;
                case RESTarMimeType.Excel:
                    var fileName = $"{Resource.Name}_output_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                    response.BodyBytes = data.ToExcel(Resource).SerializeExcel();
                    response.Headers["Content-Disposition"] = $"attachment; filename={fileName}";
                    response.ContentType = MimeTypes.Excel;
                    return;
                case RESTarMimeType.XML:
                    var json = SerializeDynamic ? data.Serialize() : data.Serialize(IEnumTypes[Resource]);
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
            if (Response == null) return null;

            ResponseHeaders.ForEach(h => Response.Headers["X-" + h.Key] = h.Value);
            Response.Headers["Access-Control-Allow-Origin"] = AllowAllOrigins ? "*" : (Origin ?? "null");

            if (Destination == null) return Response;

            var destinationRequest = HttpRequest.Parse(Destination);
            destinationRequest.ContentType = Accept.ToMimeString();
            var _response = destinationRequest.Internal
                ? HTTP.InternalRequest
                (
                    method: destinationRequest.Method,
                    relativeUri: destinationRequest.URI,
                    authToken: AuthToken,
                    bodyBytes: Response.BodyBytes,
                    contentType: destinationRequest.ContentType,
                    headers: destinationRequest.Headers
                )
                : HTTP.ExternalRequest
                (
                    method: destinationRequest.Method,
                    uri: destinationRequest.URI,
                    bodyBytes: Response.BodyBytes,
                    contentType: destinationRequest.ContentType,
                    headers: destinationRequest.Headers
                );
            if (_response == null)
                throw new Exception($"No response for destination request: '{Destination}'");
            if (!_response.IsSuccessStatusCode)
                throw new Exception($"Failed upload at destination server at '{destinationRequest.URI}'. " +
                                    $"Status: {_response.StatusCode}, {_response.StatusDescription}");
            _response.Headers["Access-Control-Allow-Origin"] = AllowAllOrigins ? "*" : (Origin ?? "null");
            return _response;
        }

        public void Dispose()
        {
            if (AuthToken == null || Internal) return;
            AuthTokens.TryRemove(AuthToken, out AccessRights _);
        }
    }
}