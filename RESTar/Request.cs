﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Dynamit;
using Excel;
using Newtonsoft.Json.Linq;
using Starcounter;
using static RESTar.ErrorCode;
using IResource = RESTar.Internal.IResource;
using ScRequest = Starcounter.Request;
using static RESTar.RESTarConfig;

namespace RESTar
{
    internal class Request : IRequest, IDisposable
    {
        internal ScRequest ScRequest { get; }
        public string AuthToken { get; private set; }
        private bool Internal => !ScRequest.IsExternal;

        public IResource Resource { get; private set; }
        public RESTarMethods Method { get; private set; }
        public Conditions Conditions { get; private set; }
        private Func<Request, Response> Evaluator { get; set; }
        public string Json { get; private set; }
        internal string Query { get; private set; }

        public int Limit { get; internal set; }
        public OrderBy OrderBy { get; private set; }
        public bool Unsafe { get; private set; }
        internal string[] Select { get; private set; }
        internal IDictionary<string, string> Rename { get; private set; }
        internal bool Dynamic { get; private set; }
        internal string Map { get; private set; }
        internal string SafePost { get; private set; }

        private string Source { get; set; }
        private string Destination { get; set; }
        private RESTarMimeType ContentType { get; set; }
        internal RESTarMimeType Accept { get; private set; }
        private byte[] BinaryBody { get; set; }
        private string Origin { get; set; }
        public IDictionary<string, string> ResponseHeaders { get; }

        internal Request(ScRequest scRequest)
        {
            ScRequest = scRequest;
            ResponseHeaders = new Dictionary<string, string>();
        }

        internal void Populate(string query, RESTarMethods method, Func<Request, Response> evaluator)
        {
            Method = method;
            query = CheckQuery(query, ScRequest);
            Evaluator = evaluator;
            Source = ScRequest.Headers["Source"];
            Destination = ScRequest.Headers["Destination"];
            Origin = ScRequest.Headers["Origin"];

            var contentType = ScRequest.ContentType?.ToLower();
            ContentType = contentType?.Contains("excel") == true ||
                          contentType?.Equals(MimeTypes.Excel) == true
                ? RESTarMimeType.Excel
                : RESTarMimeType.Json;

            var accept = ScRequest.PreferredMimeTypeString?.ToLower();
            Accept = accept?.Contains("excel") == true ||
                     accept?.ToLower().Equals(MimeTypes.Excel) == true
                ? RESTarMimeType.Excel
                : RESTarMimeType.Json;

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
            Conditions = Condition.Parse(Resource, args[2]);
            if (Conditions != null &&
                (Resource.TargetType == typeof(Resource) || Resource.TargetType.IsSubclassOf(typeof(Resource))))
            {
                var nameCond = Conditions.FirstOrDefault(c => c.Key.ToLower() == "name");
                if (nameCond != null)
                    nameCond.Value = ((string) nameCond.Value.ToString()).FindResource().Name;
            }
            if (argLength == 3) return;
            var metaConditions = MetaConditions.Parse(args[3]);
            if (metaConditions == null) return;
            Limit = metaConditions.Limit;
            Unsafe = metaConditions.Unsafe;
            Select = metaConditions.Select;
            Dynamic = metaConditions.Dynamic;
            Rename = metaConditions.Rename;
            Map = metaConditions.Map;
            SafePost = metaConditions.SafePost;
            OrderBy = metaConditions.OrderBy;
        }

        public Condition GetCondition(string key)
        {
            return Conditions?.FirstOrDefault(c => c.Key.Equals(key, StringComparison.CurrentCultureIgnoreCase));
        }

        internal void ResolveDataSource()
        {
            if (Source != null)
            {
                var sourceRequest = HttpRequest.Parse(Source);
                if (sourceRequest.Method != RESTarMethods.GET)
                    throw new SyntaxException("Only GET is allowed in Source headers", InvalidSourceFormatError);

                sourceRequest.Accept = ContentType == RESTarMimeType.Excel ? MimeTypes.Excel : MimeTypes.JSON;

                var response = sourceRequest.Internal
                    ? HTTP.InternalRequest
                    (
                        method: RESTarMethods.GET,
                        relativeUri: sourceRequest.URI,
                        authToken: AuthToken,
                        headers: sourceRequest.Headers,
                        accept: sourceRequest.Accept
                    )
                    : HTTP.ExternalRequest
                    (
                        method: RESTarMethods.GET,
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
                    Json = response.Body.RemoveTabsAndBreaks();
                    if (Json.First() == '[' && Method != RESTarMethods.POST)
                        throw new InvalidInputCountException(Resource, Method);
                    if (Json == null)
                        throw new SourceException(Source, "Response was empty");
                    return;
                }
            }
            else
            {
                if (ScRequest.Body == null &&
                    (Method == RESTarMethods.PATCH || Method == RESTarMethods.POST || Method == RESTarMethods.PUT))
                    throw new SyntaxException("Missing data source for method " + Method, NoDataSourceError);
                if (ScRequest.Body == null)
                    return;
            }

            switch (ContentType)
            {
                case RESTarMimeType.Json:
                    Json = Json?.Trim() ?? ScRequest.Body.Trim();
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
                        if (Method == RESTarMethods.POST)
                        {
                            Json = Serializer.JsonNetSerialize(result.Tables[0]);
                            Json = regex.Replace(Json, "$1$2");
                        }
                        else
                        {
                            if (result.Tables[0].Rows.Count > 1)
                                throw new InvalidInputCountException(Resource, Method);
                            Json = Serializer.JsonNetSerialize(JArray.FromObject(result.Tables[0]).First());
                        }
                    }
                    break;
            }
        }

        internal Response Evaluate() => Evaluator?.Invoke(this);

        private static readonly MethodInfo Mapper = typeof(Request).GetMethod("MapEntities",
            BindingFlags.NonPublic | BindingFlags.Static);

        internal IEnumerable<dynamic> GetExtension(bool? unsafeOverride = null)
        {
            if (unsafeOverride != null)
                Unsafe = unsafeOverride.Value;
            IEnumerable<dynamic> entities;
            try
            {
                entities = Resource.Select(this);
            }
            catch (Exception e)
            {
                throw new AbortedSelectorException(e);
            }
            if (entities == null)
                throw new NoContentException();
            if (!Unsafe && entities.Count() > 1)
                throw new AmbiguousMatchException(Resource);
            if (Select == null && Rename == null)
            {
                if (Map == null) return entities;
                if (entities is IEnumerable<DDictionary>)
                    return MapCustomEntities(this, (IEnumerable<DDictionary>) entities);
                var method = Mapper.MakeGenericMethod(Resource.TargetType);
                return (IEnumerable<dynamic>) method.Invoke(null, new object[] {this, entities});
            }
            var customEntities = entities is IEnumerable<DDictionary>
                ? SelectRenameDynamic(this, (IEnumerable<DDictionary>) entities)
                : SelectRenameStatic(this, entities);
            return MapCustomEntities(this, customEntities);
        }

        private static IEnumerable<dynamic> MapCustomEntities(Request request,
            IEnumerable<IDictionary<string, dynamic>> customEntities)
        {
            if (request.Map == null)
                return customEntities;
            return customEntities.Select(entity =>
            {
                KeyValuePair<string, dynamic> mapPair;
                try
                {
                    mapPair = entity.First(kv => string.Equals(kv.Key, request.Map,
                        StringComparison.CurrentCultureIgnoreCase));
                }
                catch (InvalidOperationException)
                {
                    throw new CustomEntityUnknownColumnException(request.Map, entity.SerializeDyn());
                }
                return new Dictionary<string, IDictionary<string, dynamic>>
                {
                    [mapPair.Value?.ToString() ?? "null"] = entity
                };
            });
        }

        private static IEnumerable<dynamic> MapEntities<T>(Request request, IEnumerable<T> entities) where T : class
        {
            if (request.Map == null)
                return entities;
            if (request.Map.ToLower() == "objectno")
                return entities.Select(entity => new Dictionary<string, T>
                {
                    [entity.GetObjectNo().ToString()] = entity
                });
            if (request.Map.ToLower() == "objectid")
                return entities.Select(entity => new Dictionary<string, T>
                {
                    [entity.GetObjectID()] = entity
                });
            return entities.Select(entity =>
            {
                object value;
                ExtensionMethods.GetValueFromKeyString(request.Resource.TargetType, request.Map, entity, out value);
                return new Dictionary<string, T>
                {
                    [value?.ToString() ?? "null"] = entity
                };
            });
        }

        private static IEnumerable<Dictionary<string, dynamic>> SelectRenameDynamic(Request request,
            IEnumerable<DDictionary> entities)
        {
            #region Select

            if (request.Select != null && request.Rename == null)
            {
                var newEntitiesList = new List<Dictionary<string, dynamic>>();
                foreach (var entity in entities)
                {
                    var newEntity = new Dictionary<string, dynamic>();
                    foreach (var s in request.Select)
                    {
                        if (s.ToLower() == "objectno")
                            newEntity["ObjectNo"] = entity.GetObjectNo();
                        else if (s.ToLower() == "objectid")
                            newEntity["ObjectID"] = entity.GetObjectID();
                        newEntity[s] = entity.SafeGet(s);
                    }
                    newEntitiesList.Add(newEntity);
                }
                return newEntitiesList;
            }

            #endregion

            #region Rename

            if (request.Select == null && request.Rename != null)
            {
                var newEntitiesList = new List<Dictionary<string, dynamic>>();
                foreach (var entity in entities)
                {
                    var newEntity = new Dictionary<string, dynamic>();
                    foreach (var pair in entity)
                    {
                        var name = pair.Key;
                        string newKey;
                        request.Rename.TryGetValue(name.ToLower(), out newKey);
                        newEntity[newKey ?? name] = entity.SafeGet(name);
                    }
                    newEntitiesList.Add(newEntity);
                }
                return newEntitiesList;
            }

            #endregion

            #region Select and Rename

            if (request.Select != null && request.Rename != null)
            {
                var newEntitiesList = new List<Dictionary<string, dynamic>>();
                foreach (var entity in entities)
                {
                    var newEntity = new Dictionary<string, dynamic>();
                    foreach (var s in request.Select)
                    {
                        if (s.ToLower() == "objectno")
                        {
                            var value = entity.GetObjectNo();
                            string newKey;
                            request.Rename.TryGetValue(s.ToLower(), out newKey);
                            newEntity[newKey ?? "ObjectNo"] = value;
                        }
                        else if (s.ToLower() == "objectid")
                        {
                            var value = entity.GetObjectID();
                            string newKey;
                            request.Rename.TryGetValue(s.ToLower(), out newKey);
                            newEntity[newKey ?? "ObjectID"] = value;
                        }
                        else
                        {
                            string newKey;
                            request.Rename.TryGetValue(s.ToLower(), out newKey);
                            newEntity[newKey ?? s] = entity.SafeGet(s);
                        }
                    }
                    newEntitiesList.Add(newEntity);
                }
                return newEntitiesList;
            }

            #endregion

            throw new ArgumentOutOfRangeException();
        }

        private static IEnumerable<Dictionary<string, dynamic>> SelectRenameStatic(Request request,
            IEnumerable<dynamic> entities)
        {
            #region Select

            if (request.Select != null && request.Rename == null)
            {
                var columns = request.Resource.TargetType.GetColumns();
                var newEntitiesList = new List<Dictionary<string, dynamic>>();
                foreach (var entity in entities)
                {
                    var newEntity = new Dictionary<string, dynamic>();
                    foreach (var s in request.Select)
                    {
                        if (s.ToLower() == "objectno")
                            newEntity["ObjectNo"] = DbHelper.GetObjectNo(entity);
                        else if (s.ToLower() == "objectid")
                            newEntity["ObjectID"] = DbHelper.GetObjectID(entity);
                        else if (s.Contains('.'))
                        {
                            dynamic value;
                            string key = ExtensionMethods.GetValueFromKeyString(request.Resource.TargetType, s, entity,
                                out value);
                            newEntity[key] = value;
                        }
                        else
                        {
                            var column = columns.FindColumn(request.Resource.TargetType, s);
                            newEntity[column.GetColumnName()] = column.GetValue(entity);
                        }
                    }
                    newEntitiesList.Add(newEntity);
                }
                return newEntitiesList;
            }

            #endregion

            #region Rename

            if (request.Select == null && request.Rename != null)
            {
                var columns = request.Resource.TargetType.GetColumns();
                var newEntitiesList = new List<Dictionary<string, dynamic>>();
                foreach (var entity in entities)
                {
                    var newEntity = new Dictionary<string, dynamic>();
                    foreach (var column in columns)
                    {
                        var name = column.GetColumnName();
                        string newKey;
                        request.Rename.TryGetValue(name.ToLower(), out newKey);
                        newEntity[newKey ?? name] = column.GetValue(entity);
                    }
                    newEntitiesList.Add(newEntity);
                }
                return newEntitiesList;
            }

            #endregion

            #region Select and Rename

            if (request.Select != null && request.Rename != null)
            {
                var columns = request.Resource.TargetType.GetColumns();
                var newEntitiesList = new List<Dictionary<string, dynamic>>();
                foreach (var entity in entities)
                {
                    var newEntity = new Dictionary<string, dynamic>();
                    foreach (var s in request.Select)
                    {
                        if (s.ToLower() == "objectno")
                        {
                            var value = DbHelper.GetObjectNo(entity);
                            string newKey;
                            request.Rename.TryGetValue(s, out newKey);
                            newEntity[newKey ?? "ObjectNo"] = value;
                        }
                        else if (s.ToLower() == "objectid")
                        {
                            var value = DbHelper.GetObjectID(entity);
                            string newKey;
                            request.Rename.TryGetValue(s, out newKey);
                            newEntity[newKey ?? "ObjectID"] = value;
                        }
                        else if (s.Contains('.'))
                        {
                            dynamic value;
                            string key = ExtensionMethods.GetValueFromKeyString(request.Resource.TargetType, s, entity,
                                out value);
                            string newKey;
                            request.Rename.TryGetValue(key.ToLower(), out newKey);
                            newEntity[newKey ?? key] = value;
                        }
                        else
                        {
                            var column = columns.FindColumn(request.Resource.TargetType, s);
                            string newKey;
                            request.Rename.TryGetValue(s, out newKey);
                            newEntity[newKey ?? column.GetColumnName()] = column.GetValue(entity);
                        }
                    }
                    newEntitiesList.Add(newEntity);
                }
                return newEntitiesList;
            }

            #endregion

            throw new ArgumentOutOfRangeException();
        }

        internal Response Respond(Response response)
        {
            ResponseHeaders.ForEach(h => response.Headers["X-" + h.Key] = h.Value);
            response.Headers["Access-Control-Allow-Origin"] = AllowAllOrigins ? "*" : (Origin ?? "null");

            if (Destination == null)
                return response;

            var destinationRequest = HttpRequest.Parse(Destination);
            destinationRequest.ContentType = Accept == RESTarMimeType.Excel ? MimeTypes.Excel : MimeTypes.JSON;
            var _response = destinationRequest.Internal
                ? HTTP.InternalRequest
                (
                    method: destinationRequest.Method,
                    relativeUri: destinationRequest.URI,
                    authToken: AuthToken,
                    bodyBytes: response.BodyBytes,
                    contentType: destinationRequest.ContentType,
                    headers: destinationRequest.Headers
                )
                : HTTP.ExternalRequest
                (
                    method: destinationRequest.Method,
                    uri: destinationRequest.URI,
                    bodyBytes: response.BodyBytes,
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