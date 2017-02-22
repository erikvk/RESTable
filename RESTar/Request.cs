using System;
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
using IResource = RESTar.Internal.IResource;
using ScRequest = Starcounter.Request;

namespace RESTar
{
    internal class Request : IRequest
    {
        private IResource _resource;

        public IResource Resource
        {
            get { return _resource; }
            private set
            {
                _resource = value;
                if (value.TargetType.IsSubclassOf(typeof(DDictionary)))
                    DynamicMemberResource = true;
             }
        }

        internal bool DynamicMemberResource;
        public IList<Condition> Conditions { get; private set; }
        public IDictionary<string, object> MetaConditions { get; }
        internal readonly ScRequest ScRequest;
        internal readonly string Query;
        public bool Unsafe { get; private set; }
        public int Limit { get; set; } = -1;
        public OrderBy OrderBy { get; }
        internal readonly string[] Select;
        internal readonly IDictionary<string, string> Rename;
        internal readonly string Source;
        internal readonly string Destination;
        internal string Json;
        internal byte[] BinaryBody;
        internal readonly bool Dynamic;
        internal readonly string Map;
        internal readonly string SafePost;
        internal readonly RESTarMimeType InputMimeType;
        internal readonly RESTarMimeType OutputMimeType;
        internal string Imgput;
        public RESTarMethods Method { get; set; }
        public Func<Request, Response> Evaluator;

        internal Request(ScRequest scRequest, string query, RESTarMethods method, Func<Request, Response> evaluator)
        {
            if (query == null)
                throw new RESTarInternalException("Query not loaded");
            if (query.CharCount('/') > 3)
                throw new SyntaxException("Invalid argument separator count. A RESTar URI can contain at most 3 " +
                                          $"forward slashes after the base uri. URI scheme: {Settings._ResourcesPath}" +
                                          "/[resource]/[conditions]/[meta-conditions]");
            Evaluator = evaluator;
            Query = query;
            ScRequest = scRequest;
            Method = method;
            Source = scRequest.Headers["Source"];
            Destination = scRequest.Headers["Destination"];
  
            var ContentType = scRequest.ContentType?.ToLower();
            InputMimeType = ContentType?.Contains("excel") == true ||
                            ContentType?.Equals("application/vnd.openxmlformats-" +
                                                "officedocument.spreadsheetml.sheet") == true
                ? RESTarMimeType.Excel
                : RESTarMimeType.Json;

            var Accept = scRequest.PreferredMimeTypeString?.ToLower();
            OutputMimeType = Accept?.Contains("excel") == true ||
                             Accept?.ToLower().Equals("application/vnd.openxmlformats-" +
                                                      "officedocument.spreadsheetml.sheet") == true
                ? RESTarMimeType.Excel
                : RESTarMimeType.Json;

            #region Parse arguments

            var args = Query.Split('/');
            var argLength = args.Length;

            if (argLength == 1)
            {
                Resource = RESTarConfig.TypeResources[typeof(Resource)];
                return;
            }

            if (args[1] == "")
                Resource = RESTarConfig.TypeResources[typeof(Resource)];
            else Resource = args[1].FindResource();
            if (argLength == 2) return;

            Conditions = Condition.ParseConditions(Resource, args[2]);
            if (Conditions != null &&
                (Resource.TargetType == typeof(Resource) || Resource.TargetType.IsSubclassOf(typeof(Resource))))
            {
                var nameCond = Conditions.FirstOrDefault(c => c.Key.ToLower() == "name");
                if (nameCond != null)
                    nameCond.Value = ((string) nameCond.Value.ToString()).FindResource().Name;
            }
            if (argLength == 3) return;

            MetaConditions = Condition.ParseMetaConditions(args[3]);
            if (MetaConditions == null) return;

            if (MetaConditions.ContainsKey("limit"))
                Limit = decimal.ToInt32((int) MetaConditions["limit"]);
            if (MetaConditions.ContainsKey("unsafe"))
                Unsafe = (bool) MetaConditions["unsafe"];
            if (MetaConditions.ContainsKey("select"))
                Select = ((string) MetaConditions["select"]).Split(',').ToArray();
            if (MetaConditions.ContainsKey("dynamic"))
                Dynamic = (bool) MetaConditions["dynamic"];
            if (MetaConditions.ContainsKey("rename"))
                Rename = ((string) MetaConditions["rename"]).Split(',').ToDictionary(
                    pair => pair.Split(new[] {"->"}, StringSplitOptions.None)[0].ToLower(),
                    pair => pair.Split(new[] {"->"}, StringSplitOptions.None)[1]
                );
            if (MetaConditions.ContainsKey("map"))
                Map = (string) MetaConditions["map"];
            if (MetaConditions.ContainsKey("safepost"))
                SafePost = (string) MetaConditions["safepost"];
            var orderKey = MetaConditions.Keys.FirstOrDefault(key => key.Contains("order"));
            if (orderKey == null) return;
            OrderBy = new OrderBy
            {
                Descending = orderKey.Contains("desc"),
                Key = MetaConditions[orderKey].ToString()
            };

            #endregion
        }

        public Condition GetCondition(string key)
        {
            return Conditions?.FirstOrDefault(c => c.Key.Equals(key, StringComparison.CurrentCultureIgnoreCase));
        }

        internal void ResolveMethod()
        {
            if (Method == RESTarMethods.GET && MetaConditions?.ContainsKey("imgput") == true)
            {
                Method = RESTarMethods.PUT;
                Imgput = (string) MetaConditions["imgput"];
                Evaluator = Evaluators.PUT;
            }
        }

        internal void ResolveDataSource()
        {
            if (Imgput != null)
            {
                if (Conditions == null || !Conditions.Any())
                    throw new SyntaxException("Missing data source for method " + Method);
                try
                {
                    var dict = Conditions.ToConditionsDict();
                    var keys = Imgput.Split(',');
                    Conditions = keys.Select(key =>
                    {
                        var match = dict.FirstOrDefault(pair =>
                            string.Equals(pair.Key, key, StringComparison.CurrentCultureIgnoreCase));
                        return new Condition
                        {
                            Key = match.Key,
                            Operator = new Operator("=", "="),
                            Value = match.Value
                        };
                    }).ToList();
                    Json = dict.SerializeDyn();
                    return;
                }
                catch (KeyNotFoundException)
                {
                    throw new Exception("Invalid imgput request. One of the property " +
                                        "locators was not found among conditions.");
                }
            }

            if (Source != null)
            {
                var method_uri = Source.Split(new[] {' '}, 2);
                if (method_uri.Length == 1 || method_uri[0].ToUpper() != "GET")
                    throw new SyntaxException("Source must be of form 'GET [URI]'");
                var response = HTTP.Request
                (
                    method: method_uri[0],
                    uri: method_uri[1],
                    bodyString: null,
                    contentType: null,
                    accept: InputMimeType == RESTarMimeType.Excel
                        ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                        : "application/json"
                );
                if (response?.IsSuccessStatusCode != true)
                    throw new ExternalSourceException(Source,
                        $"{response?.StatusCode}: " +
                        $"{response?.StatusDescription}"
                    );
                if (InputMimeType == RESTarMimeType.Excel)
                {
                    BinaryBody = response.BodyBytes;
                    if (BinaryBody?.Any() != true)
                        throw new ExternalSourceException(Source, "Response was empty");
                }
                else
                {
                    Json = response.Body.RemoveTabsAndBreaks();
                    if (Json.First() == '[' && Method != RESTarMethods.POST)
                        throw new InvalidInputCountException(Resource, Method);
                    if (Json == null)
                        throw new ExternalSourceException(Source, "Response was empty");
                    return;
                }
            }
            else
            {
                if (ScRequest.Body == null &&
                    (Method == RESTarMethods.PATCH || Method == RESTarMethods.POST || Method == RESTarMethods.PUT))
                    throw new SyntaxException("Missing data source for method " + Method);
                if (ScRequest.Body == null)
                    return;
            }

            switch (InputMimeType)
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
                throw new AbortedSelectorException(e.Message);
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
                            string key = ExtensionMethods.GetValueFromKeyString(request.Resource.TargetType, s, entity, out value);
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
                            string key = ExtensionMethods.GetValueFromKeyString(request.Resource.TargetType, s, entity, out value);
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

        internal Response GetResponse(Response response)
        {
            if (Destination != null)
            {
                var method_uri = Destination.Split(new[] {' '}, 2);
                if (method_uri.Length == 1)
                    throw new SyntaxException("Destination must be of form '[METHOD] [URI]'");
                return HTTP.Request
                (
                    method: method_uri[0].ToUpper(),
                    uri: method_uri[1],
                    bodyString: response.Body,
                    contentType: InputMimeType == RESTarMimeType.Excel
                        ? "application/vnd-ms.excel"
                        : "application/json"
                );
            }
            return response;
        }
    }
}