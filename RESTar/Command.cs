using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public string ExternalSource;
        public string Json;
        public bool Dynamic;
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
            ExternalSource = request.Headers["ExternalSource"];
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
            if (ExternalSource != null)
            {
                var response = HTTP.GET(ExternalSource);
                if (!response.IsSuccessStatusCode)
                    throw new ExternalSourceException(ExternalSource,
                        $"{response.StatusCode}: " +
                        $"{response.StatusDescription}"
                    );
                Json = response.Body.RemoveTabsAndBreaks();
                if (Json.First() == '[' && Method != RESTarMethods.POST)
                    throw new InvalidInputCountException(Resource, Method);
                if (Json == null)
                    throw new ExternalSourceException(ExternalSource, "Response was empty");
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
            if (unsafeOverride != null)
                Unsafe = unsafeOverride.Value;
            switch (ResourceType)
            {
                case ResourceType.Starcounter:
                    if (Unsafe)
                        return DB.GetStatic(Resource, Conditions.ToWhereClause(), Limit, OrderBy);
                    dynamic items = DB.GetStatic(Resource, Conditions.ToWhereClause(), 2, OrderBy);
                    if (Enumerable.Count(items) > 1) throw new AmbiguousMatchException(Resource);
                    return items;
                case ResourceType.Virtual:
                    if (Unsafe)
                        return (IEnumerable<dynamic>) Resource.GetMethod("Get").Invoke(null, new object[] {Conditions});
                    dynamic _items = Resource.GetMethod("Get").Invoke(null, new object[] {Conditions});
                    if (Enumerable.Count(_items) > 1) throw new AmbiguousMatchException(Resource);
                    return _items;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}