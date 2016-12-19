using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Excel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Starcounter;

namespace RESTar
{
    internal class Command
    {
        private Type _resource;

        public Type Resource
        {
            get { return _resource; }
            private set
            {
                _resource = value;
                ResourceType = _resource.HasAttribute<DatabaseAttribute>()
                    ? ResourceType.Starcounter
                    : ResourceType.Virtual;
            }
        }

        public ResourceType ResourceType;
        public IList<Condition> Conditions;
        public IDictionary<string, object> MetaConditions;
        public Request Request;
        public readonly string Query;
        public bool Unsafe;
        public int Limit = -1;
        public OrderBy OrderBy;
        public string[] Select;
        public IDictionary<string, string> Rename;
        public string Source;
        public string Destination;
        public string Json;
        public bool Dynamic;
        public string Map;
        public RESTarMimeType InputMimeType;
        public RESTarMimeType OutputMimeType;
        public RESTarMethods Method;

        internal Command(Request request, string query, RESTarMethods method)
        {
            if (query == null)
                throw new RESTarInternalException("Query not loaded");
            if (query.CharCount('/') > 3)
                throw new SyntaxException("Invalid argument separator count. A RESTar URI can contain at most 3 " +
                                          $"forward slashes after the base uri. URI scheme: {Settings._ResourcesPath}" +
                                          "/[resource]/[conditions]/[meta-conditions]");
            Query = query;
            Request = request;
            Method = method;
            Source = request.Headers["Source"];
            Destination = request.Headers["Destination"];
            InputMimeType = request.ContentType?.ToLower().Contains("excel") == true
                ? RESTarMimeType.Excel
                : RESTarMimeType.Json;
            OutputMimeType = request.PreferredMimeTypeString?.ToLower().Contains("excel") == true
                ? RESTarMimeType.Excel
                : RESTarMimeType.Json;

            #region Parse arguments

            var args = Query.Split('/');
            var argLength = args.Length;

            if (argLength == 1)
            {
                Resource = typeof(Resource);
                return;
            }

            if (args[1] == "")
                Resource = typeof(Resource);
            else Resource = args[1].FindResource();
            if (argLength == 2) return;

            Conditions = Condition.ParseConditions(Resource, args[2]);
            if (Conditions != null &&
                (Resource == typeof(Resource) || Resource.IsSubclassOf(typeof(Resource))))
            {
                var nameCondition = Conditions.FirstOrDefault(c => c.Key.ToLower() == "name");
                if (nameCondition != null)
                    nameCondition.Value = nameCondition.Value.ToString().FindResource().FullName;
            }
            if (argLength == 3) return;

            MetaConditions = Condition.ParseMetaConditions(args[3]);
            if (MetaConditions == null) return;

            if (MetaConditions.ContainsKey("limit"))
                Limit = decimal.ToInt32((decimal) MetaConditions["limit"]);
            if (MetaConditions.ContainsKey("unsafe"))
                Unsafe = (bool) MetaConditions["unsafe"];
            if (MetaConditions.ContainsKey("select"))
                Select = ((string) MetaConditions["select"]).Split(',').Select(s => s.ToLower()).ToArray();
            if (MetaConditions.ContainsKey("dynamic"))
                Dynamic = (bool) MetaConditions["dynamic"];
            if (MetaConditions.ContainsKey("rename"))
                Rename = ((string) MetaConditions["rename"]).Split(',').ToDictionary(
                    pair => pair.Split(new[] {"->"}, StringSplitOptions.None)[0].ToLower(),
                    pair => pair.Split(new[] {"->"}, StringSplitOptions.None)[1]
                );
            if (MetaConditions.ContainsKey("map"))
                Map = (string) MetaConditions["map"];
            var orderKey = MetaConditions.Keys.FirstOrDefault(key => key.Contains("order"));
            if (orderKey == null) return;
            OrderBy = new OrderBy
            {
                Descending = orderKey.Contains("desc"),
                Key = MetaConditions[orderKey].ToString()
            };

            #endregion
        }

        internal void ResolveDataSource()
        {
            if (Source != null)
            {
                var method_uri = Source.Split(new[] {' '}, 2);
                if (method_uri.Length == 1 || method_uri[0].ToUpper() != "GET")
                    throw new SyntaxException("Source must be of form 'GET [URI]'");
                var response = HTTP.INNER(method_uri[0], method_uri[1], null);
                if (!response.IsSuccessStatusCode)
                    throw new ExternalSourceException(Source,
                        $"{response.StatusCode}: " +
                        $"{response.StatusDescription}"
                    );
                Json = response.Body.RemoveTabsAndBreaks();
                if (Json.First() == '[' && Method != RESTarMethods.POST)
                    throw new InvalidInputCountException(Resource, Method);
                if (Json == null)
                    throw new ExternalSourceException(Source, "Response was empty");
                return;
            }

            if (Request.BodyBytes == null &&
                (Method == RESTarMethods.PATCH ||
                 Method == RESTarMethods.POST ||
                 Method == RESTarMethods.PUT))
            {
                throw new SyntaxException("Missing data source for method " + Method);
            }

            if (Request.BodyBytes == null)
                return;

            switch (InputMimeType)
            {
                case RESTarMimeType.Json:
                    Json = Request.Body;
                    break;
                case RESTarMimeType.Excel:
                    using (var stream = new MemoryStream(Request.BodyBytes))
                    {
                        var regex = new Regex(@"(:[\d]+).0([\D])");
                        var excelReader = ExcelReaderFactory.CreateOpenXmlReader(stream);
                        excelReader.IsFirstRowAsColumnNames = true;
                        var result = excelReader.AsDataSet();
                        if (result == null)
                            throw new ExcelInputException();
                        if (Method == RESTarMethods.POST)
                        {
                            Json = JsonConvert.SerializeObject(result.Tables[0]);
                            Json = regex.Replace(Json, "$1$2");
                        }
                        else
                        {
                            if (result.Tables[0].Rows.Count > 1)
                                throw new InvalidInputCountException(Resource, Method);
                            Json = JsonConvert.SerializeObject(JArray.FromObject(result.Tables[0]).First());
                        }
                    }
                    break;
            }
        }

        internal IEnumerable<dynamic> GetExtension(bool? unsafeOverride = null)
        {
            IEnumerable<dynamic> entities;
            if (unsafeOverride != null)
                Unsafe = unsafeOverride.Value;
            switch (ResourceType)
            {
                case ResourceType.Starcounter:
                    entities = DB.GetStatic(Resource, Conditions.ToWhereClause(), Limit, OrderBy);
                    if (Unsafe) break;
                    if (entities.Count() > 1) throw new AmbiguousMatchException(Resource);
                    break;
                case ResourceType.Virtual:
                    entities = (IEnumerable<dynamic>) Resource.GetMethod("Get").Invoke(null, new object[] {Conditions});
                    if (Unsafe) break;
                    if (entities.Count() > 1) throw new AmbiguousMatchException(Resource);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (Select == null && Rename == null)
            {
                if (Map == null)
                    return entities;
                var method =
                    typeof(Command).GetMethod("MapEntities", BindingFlags.NonPublic | BindingFlags.Static)
                        .MakeGenericMethod(Resource);
                return (IEnumerable<dynamic>) method.Invoke(null, new object[] {this, entities});
            }
            var customEntities = SelectRename(this, entities);
            return MapCustomEntities(this, customEntities);
        }

        private static IEnumerable<dynamic> MapCustomEntities(Command command,
            IEnumerable<IDictionary<string, dynamic>> customEntities)
        {
            if (command.Map == null)
                return customEntities;
            var dictList = (IEnumerable<Dictionary<string, dynamic>>) customEntities;
            return dictList.Select(entity =>
            {
                KeyValuePair<string, dynamic> mapPair;
                try
                {
                    mapPair = entity.First(kv => string.Equals(kv.Key, command.Map,
                        StringComparison.CurrentCultureIgnoreCase));
                }
                catch (InvalidOperationException)
                {
                    throw new CustomEntityUnknownColumnException(command.Map, entity.SerializeDyn());
                }
                return new Dictionary<string, Dictionary<string, dynamic>>
                {
                    [mapPair.Value?.ToString() ?? "null"] = entity
                };
            });
        }

        private static IEnumerable<dynamic> MapEntities<T>(Command command, IEnumerable<T> entities) where T : class
        {
            if (command.Map == null)
                return entities;
            if (command.Map.ToLower() == "objectno")
                return entities.Select(entity => new Dictionary<string, T>
                {
                    [entity.GetObjectNo().ToString()] = entity
                });
            if (command.Map.ToLower() == "objectid")
                return entities.Select(entity => new Dictionary<string, T>
                {
                    [entity.GetObjectID()] = entity
                });
            return entities.Select(entity =>
            {
                object value;
                ExtensionMethods.GetValueFromKeyString(command.Resource, command.Map, entity, out value);
                return new Dictionary<string, T>
                {
                    [value?.ToString() ?? "null"] = entity
                };
            });
        }

        private static IEnumerable<Dictionary<string, dynamic>> SelectRename(Command command,
            IEnumerable<dynamic> entities)
        {
            #region Select

            if (command.Select != null && command.Rename == null)
            {
                var columns = command.Resource.GetColumns();
                var newEntitiesList = new List<Dictionary<string, dynamic>>();
                foreach (var entity in entities)
                {
                    var newEntity = new Dictionary<string, dynamic>();
                    foreach (var s in command.Select)
                    {
                        if (s.ToLower() == "objectno")
                            newEntity["ObjectNo"] = DbHelper.GetObjectNo(entity);
                        else if (s.ToLower() == "objectid")
                            newEntity["ObjectID"] = DbHelper.GetObjectID(entity);
                        else if (s.Contains('.'))
                        {
                            dynamic value;
                            string key = ExtensionMethods.GetValueFromKeyString(command.Resource, s, entity, out value);
                            newEntity[key] = value;
                        }
                        else
                        {
                            var column = columns.FindColumn(command.Resource, s);
                            newEntity[column.GetColumnName()] = column.GetValue(entity);
                        }
                    }
                    newEntitiesList.Add(newEntity);
                }
                return newEntitiesList;
            }

            #endregion

            #region Rename

            if (command.Select == null && command.Rename != null)
            {
                var columns = command.Resource.GetColumns();
                var newEntitiesList = new List<Dictionary<string, dynamic>>();
                foreach (var entity in entities)
                {
                    var newEntity = new Dictionary<string, dynamic>();
                    foreach (var column in columns)
                    {
                        var name = column.GetColumnName();
                        string newKey;
                        command.Rename.TryGetValue(name.ToLower(), out newKey);
                        newEntity[newKey ?? name] = column.GetValue(entity);
                    }
                    newEntitiesList.Add(newEntity);
                }
                return newEntitiesList;
            }

            #endregion

            #region Select and Rename

            if (command.Select != null && command.Rename != null)
            {
                var columns = command.Resource.GetColumns();
                var newEntitiesList = new List<Dictionary<string, dynamic>>();
                foreach (var entity in entities)
                {
                    var newEntity = new Dictionary<string, dynamic>();
                    foreach (var s in command.Select)
                    {
                        if (s.ToLower() == "objectno")
                        {
                            var value = DbHelper.GetObjectNo(entity);
                            string newKey;
                            command.Rename.TryGetValue(s, out newKey);
                            newEntity[newKey ?? "ObjectNo"] = value;
                        }
                        else if (s.ToLower() == "objectid")
                        {
                            var value = DbHelper.GetObjectID(entity);
                            string newKey;
                            command.Rename.TryGetValue(s, out newKey);
                            newEntity[newKey ?? "ObjectID"] = value;
                        }
                        else if (s.Contains('.'))
                        {
                            dynamic value;
                            string key = ExtensionMethods.GetValueFromKeyString(command.Resource, s, entity, out value);
                            string newKey;
                            command.Rename.TryGetValue(key.ToLower(), out newKey);
                            newEntity[newKey ?? key] = value;
                        }
                        else
                        {
                            var column = columns.FindColumn(command.Resource, s);
                            string newKey;
                            command.Rename.TryGetValue(s, out newKey);
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

        internal void SendResponse(Response response)
        {
            if (Destination != null)
            {
                var method_uri = Destination.Split(new[] {' '}, 2);
                if (method_uri.Length == 1)
                    throw new SyntaxException("Destination must be of form '[METHOD] [URI]'");
                Request.SendResponse(HTTP.INNER(method_uri[0].ToUpper(), method_uri[1], response.Body), null);
            }
            Request.SendResponse(response, null);
        }
    }
}