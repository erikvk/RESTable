using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using ClosedXML.Excel;
using Dynamit;
using Excel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RESTar.Internal;
using Starcounter;
using static System.StringComparison;
using static RESTar.ErrorCode;
using static RESTar.PostOperations;
using IResource = RESTar.Internal.IResource;
using ScRequest = Starcounter.Request;
using static RESTar.RESTarConfig;
using static RESTar.RESTarMethods;

namespace RESTar
{
    internal class Request : IRequest, IDisposable
    {
        internal ScRequest ScRequest { get; }
        internal Response Response { get; private set; }
        public string AuthToken { get; private set; }
        private bool Internal => !ScRequest.IsExternal;
        internal Transaction Transaction { get; }

        public IResource Resource { get; private set; }
        public RESTarMethods Method { get; private set; }
        public Conditions Conditions { get; private set; }
        public MetaConditions MetaConditions { get; private set; }
        private Func<Request, Response> Evaluator { get; set; }
        internal void Evaluate() => Response = Evaluator?.Invoke(this);
        public string Body { get; private set; }
        private byte[] BinaryBody { get; set; }
        internal string Query { get; private set; }

        private bool SerializeDynamic => MetaConditions.Dynamic || MetaConditions.Select != null ||
                                         MetaConditions.Rename != null || MetaConditions.Add != null ||
                                         Resource.TargetType.IsSubclassOf(typeof(DDictionary)) ||
                                         Resource.TargetType.GetAttribute<RESTarAttribute>()?.Dynamic == true;

        private string Source { get; set; }
        private string Destination { get; set; }
        private RESTarMimeType ContentType { get; set; }
        internal RESTarMimeType Accept { get; private set; }
        private string Origin { get; set; }
        public IDictionary<string, string> ResponseHeaders { get; }

        internal Request(ScRequest scRequest)
        {
            ScRequest = scRequest;
            ResponseHeaders = new Dictionary<string, string>();
            MetaConditions = new MetaConditions();
            Transaction = new Transaction();
        }

        internal void Populate(string query, RESTarMethods method, Func<Request, Response> evaluator)
        {
            Method = method;
            query = CheckQuery(query, ScRequest);
            Evaluator = evaluator;
            Source = ScRequest.Headers["Source"];
            Destination = ScRequest.Headers["Destination"];
            Origin = ScRequest.Headers["Origin"];

            ContentType = MimeTypes.Match(ScRequest.ContentType);
            Accept = MimeTypes.Match(ScRequest.PreferredMimeTypeString);

            var args = query.Split('/');
            var argLength = args.Length;
            if (argLength == 1)
            {
                Resource = TypeResources[typeof(Resource)];
                return;
            }
            if (args[1] == "")
                Resource = TypeResources[typeof(Resource)];
            else Resource = args[1].FindResource();
            if (argLength == 2) return;

            Conditions = Conditions.Parse(args[2], Resource);
            if (Conditions != null &&
                (Resource.TargetType == typeof(Resource) || Resource.TargetType.IsSubclassOf(typeof(Resource))))
            {
                var nameCond = Conditions["name"];
                if (nameCond != null)
                    nameCond.Value = ((string) nameCond.Value.ToString()).FindResource().Name;
            }
            if (argLength == 3) return;

            MetaConditions = MetaConditions.Parse(args[3], Resource);
        }

        internal void GetRequestData()
        {
            if (Source != null)
            {
                var sourceRequest = HttpRequest.Parse(Source);
                if (sourceRequest.Method != GET)
                    throw new SyntaxException("Only GET is allowed in Source headers", InvalidSourceFormatError);

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
                if (ScRequest.Body == null &&
                    (Method == PATCH || Method == POST || Method == PUT))
                    throw new SyntaxException("Missing data source for method " + Method, NoDataSourceError);
                if (ScRequest.Body == null)
                    return;
            }

            switch (ContentType)
            {
                case RESTarMimeType.Json:
                    Body = Body?.Trim() ?? ScRequest.Body.Trim();
                    if (Body?.First() == '[' && Method != POST)
                        throw new InvalidInputCountException(Resource, Method);
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
                            Body = result.Tables[0].JsonNetSerialize();
                            Body = regex.Replace(Body, "$1$2");
                        }
                        else
                        {
                            if (result.Tables[0].Rows.Count > 1)
                                throw new InvalidInputCountException(Resource, Method);
                            Body = JArray.FromObject(result.Tables[0]).First().JsonNetSerialize();
                        }
                    }
                    break;
                case RESTarMimeType.XML:
                    throw new FormatException("XML is only supported as output format");
            }
        }

        internal void SetResponseData(IEnumerable<dynamic> entities, Response response)
        {
            if (Accept == RESTarMimeType.Excel)
            {
                var data = entities.ToDataSet();
                var workbook = new XLWorkbook();
                workbook.AddWorksheet(data);
                var fileName = $"{Resource.Name}_export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                using (var memstream = new MemoryStream())
                {
                    workbook.SaveAs(memstream);
                    response.BodyBytes = memstream.ToArray();
                }
                response.Headers["Content-Disposition"] = $"attachment; filename={fileName}";
                response.ContentType = MimeTypes.Excel;
                return;
            }

            string jsonString;
            if (SerializeDynamic)
                jsonString = entities.SerializeDyn();
            else jsonString = entities.Serialize(IEnumTypes[Resource]);

            switch (Accept)
            {
                case RESTarMimeType.Json:
                    response.Body = jsonString;
                    response.ContentType = MimeTypes.JSON;
                    break;
                case RESTarMimeType.XML:
                    var xml = JsonConvert.DeserializeXmlNode($@"{{""row"":{jsonString}}}", "root", true);
                    response.Body = xml.SerializeXml();
                    response.ContentType = MimeTypes.XML;
                    break;
                default: throw new ArgumentOutOfRangeException(nameof(Accept));
            }
        }

        internal IEnumerable<dynamic> PostProcess(IEnumerable<dynamic> entities)
        {
            switch (MetaConditions.PostOperation)
            {
                case NoOperation: return entities;
                case Select:
                    return entities.Select(entity => MetaConditions.Select.ToDictionary(
                        prop => prop.Key, prop => prop.GetValue(entity)));
                case Rename:
                    return entities.Select(entity =>
                    {
                        IDictionary<string, dynamic> dict = ExtensionMethods.MakeDictionary(entity);
                        MetaConditions.Rename.ForEach(pair =>
                        {
                            var oldPropertyName = pair.Key.Key;
                            var newPropertyName = pair.Value;
                            string actualKey;
                            if (dict.ContainsKeyIgnorecase(oldPropertyName, out actualKey))
                            {
                                var value = dict[actualKey];
                                dict.Remove(actualKey);
                                dict[newPropertyName] = value;
                            }
                        });
                        return dict;
                    });
                case SelectRename:
                    return entities.Select(entity => MetaConditions.Select.ToDictionary(
                        prop => MetaConditions.Rename
                            .FirstOrDefault(pair => string.Equals(pair.Key.Key, prop.Key, CurrentCultureIgnoreCase))
                            .Value, prop => prop.GetValue(entity)));
                case Add:
                    return entities.Select(entity =>
                    {
                        IDictionary<string, dynamic> dict = ExtensionMethods.MakeDictionary(entity);
                        MetaConditions.Add.ForEach(propToAdd =>
                        {
                            if (!dict.ContainsKey(propToAdd.Key))
                                dict[propToAdd.Key] = propToAdd.GetValue(entity);
                        });
                        return dict;
                    });
                case SelectAdd:
                    return entities.Select(entity =>
                    {
                        var dict = MetaConditions.Select.ToDictionary(s => s.Key, s => s.GetValue(entity));
                        MetaConditions.Add.ForEach(propToAdd =>
                        {
                            if (!dict.ContainsKey(propToAdd.Key))
                                dict[propToAdd.Key] = propToAdd.GetValue(entity);
                        });
                        return dict;
                    });
                case RenameAdd:
                    return entities.Select(entity =>
                    {
                        IDictionary<string, dynamic> dict = ExtensionMethods.MakeDictionary(entity);
                        MetaConditions.Add.ForEach(propToAdd =>
                        {
                            if (!dict.ContainsKey(propToAdd.Key))
                                dict[propToAdd.Key] = propToAdd.GetValue(entity);
                        });
                        MetaConditions.Rename.ForEach(pair =>
                        {
                            var oldPropertyName = pair.Key.Key;
                            var newPropertyName = pair.Value;
                            string actualKey;
                            if (dict.ContainsKeyIgnorecase(oldPropertyName, out actualKey))
                            {
                                var value = dict[actualKey];
                                dict.Remove(actualKey);
                                dict[newPropertyName] = value;
                            }
                        });
                        return dict;
                    });
                case SelectRenameAdd:
                    return entities.Select(entity =>
                    {
                        var dict = MetaConditions.Select.ToDictionary(prop => prop.Key, prop => prop.GetValue(entity));
                        MetaConditions.Add.ForEach(propToAdd =>
                        {
                            if (!dict.ContainsKey(propToAdd.Key))
                                dict[propToAdd.Key] = propToAdd.GetValue(entity);
                        });
                        MetaConditions.Rename.ForEach(pair =>
                        {
                            var oldPropertyName = pair.Key.Key;
                            var newPropertyName = pair.Value;
                            string actualKey;
                            if (dict.ContainsKeyIgnorecase(oldPropertyName, out actualKey))
                            {
                                var value = dict[actualKey];
                                dict.Remove(actualKey);
                                dict[newPropertyName] = value;
                            }
                        });
                        return dict;
                    });
                default: throw new ArgumentOutOfRangeException();
            }
        }

        internal Response GetResponse()
        {
            ResponseHeaders.ForEach(h => Response.Headers["X-" + h.Key] = h.Value);
            Response.Headers["Access-Control-Allow-Origin"] = AllowAllOrigins ? "*" : (Origin ?? "null");

            if (Destination == null)
                return Response;

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

        internal void Authenticate()
        {
            if (!RequireApiKey)
                return;

            AccessRights accessRights;

            if (!ScRequest.IsExternal)
            {
                var authToken = ScRequest.Headers["RESTar-AuthToken"];
                if (string.IsNullOrWhiteSpace(authToken))
                    throw new ForbiddenException();
                if (!AuthTokens.TryGetValue(authToken, out accessRights))
                    throw new ForbiddenException();
                AuthToken = authToken;
                return;
            }

            var authorizationHeader = ScRequest.Headers["Authorization"];
            if (string.IsNullOrWhiteSpace(authorizationHeader))
                throw new ForbiddenException();
            var apikey_key = authorizationHeader.Split(' ');
            if (apikey_key[0].ToLower() != "apikey" || apikey_key.Length != 2)
                throw new ForbiddenException();
            var apiKey = apikey_key[1].SHA256();
            if (!ApiKeys.TryGetValue(apiKey, out accessRights))
                throw new ForbiddenException();
            AuthToken = Guid.NewGuid().ToString();
            AuthTokens[AuthToken] = accessRights;
        }

        internal void MethodCheck()
        {
            var method = Method;
            var availableMethods = Resource.AvailableMethods;
            if (!availableMethods.Contains(method))
                throw new ForbiddenException();
            if (!RequireApiKey)
                return;
            var accessRights = AuthTokens[AuthToken];
            if (accessRights == null)
                throw new ForbiddenException();
            var rights = accessRights[Resource];
            if (rights == null || !rights.Contains(method))
                throw new ForbiddenException();
        }

        public void Dispose()
        {
            if (AuthToken == null || Internal) return;
            AccessRights accessRights;
            AuthTokens.TryRemove(AuthToken, out accessRights);
        }

        private static string CheckQuery(string query, ScRequest request)
        {
            if (query.CharCount('/') > 3)
                throw new SyntaxException("Invalid argument separator count. A RESTar URI can contain at most 3 " +
                                          $"forward slashes after the base uri. URI scheme: {Settings._ResourcesPath}" +
                                          "/[resource]/[conditions]/[meta-conditions]", InvalidSeparatorCount);
            if (request.HeadersDictionary.ContainsKey("X-ARR-LOG-ID"))
                return query.Replace("%25", "%");
            return query;
        }
    }
}