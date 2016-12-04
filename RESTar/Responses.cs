using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Starcounter;
using System.Data;
using System.IO;
using System.Reflection;
using ClosedXML.Excel;
using Newtonsoft.Json;

namespace RESTar
{
    internal static class Responses
    {
        #region Not found

        internal static Response AmbiguousResource(AmbiguousResourceException e) => new Response
        {
            StatusCode = (ushort) HttpStatusCode.NotFound,
            StatusDescription = e.Message + "Try qualifying the resource locator further, e.g. from " +
                                $"'{e.SearchString}' to '{e.Candidates.First()}'."
        };

        internal static Response UnknownResource(UnknownResourceException e) => new Response
        {
            StatusCode = (ushort) HttpStatusCode.NotFound,
            StatusDescription = e.Message
        };

        internal static Response AmbiguousColumn(AmbiguousColumnException e) => new Response
        {
            StatusCode = (ushort) HttpStatusCode.NotFound,
            StatusDescription = e.Message + "Try qualifying the column name further, e.g. from " +
                                $"'{e.SearchString}' to '{e.Candidates.First()}'."
        };

        internal static Response UnknownColumn(UnknownColumnException e) => new Response
        {
            StatusCode = (ushort) HttpStatusCode.NotFound,
            StatusDescription = e.Message
        };

        #endregion

        #region Bad request

        internal static Response SyntaxError(SyntaxException e) => new Response
        {
            StatusCode = (ushort) HttpStatusCode.BadRequest,
            StatusDescription = e.Message
        };

        internal static Response SemanticsError(SqlException e) => new Response
        {
            StatusCode = (ushort) HttpStatusCode.BadRequest,
            StatusDescription = $"{e.Message}To enumerate available sub-resources (e.g. columns in a table) " +
                                $"for a RESTar resource R: GET {Settings._ResourcesPath}/RESTar.resource/name=R"
        };

        internal static Response DeserializationError(string json) => new Response
        {
            StatusCode = (ushort) HttpStatusCode.BadRequest,
            StatusDescription = $"Error while deserializing JSON. Check JSON syntax. JSON: {json}"
        };

        internal static Response BlockedMethod(RESTarMethods method, Type resource) => new Response
        {
            StatusCode = (ushort) HttpStatusCode.Forbidden,
            StatusDescription = $"{method} is blocked for resource '{resource.FullName}'. Available " +
                                $"methods: {resource.AvailableMethods().ToMethodsString()}"
        };

        internal static Response ExternalSourceError(ExternalSourceException e) => new Response
        {
            StatusCode = (ushort) HttpStatusCode.BadRequest,
            StatusDescription = e.Message
        };

        #endregion

        #region Internal

        internal static Response UnknownError(Exception e) => new Response
        {
            StatusCode = (ushort) HttpStatusCode.InternalServerError,
            StatusDescription = $"Unknown error: {e.Message}"
        };

        internal static Response RESTarInternalError(Exception e) => new Response
        {
            StatusCode = (ushort) HttpStatusCode.InternalServerError,
            StatusDescription = $"Internal RESTar error: {e.Message}"
        };

        #endregion

        #region Ambiguous

        internal static Response AmbiguousMatch(Type resource) => new Response
        {
            StatusCode = (ushort) HttpStatusCode.Conflict,
            StatusDescription =
                $"Expected a uniquely matched entity in resource '{resource.FullName}' for this command, " +
                "but matched multiple entities satisfying the given conditions. To enable manipulation of " +
                "multiple matched entities (for commands that support this), add 'unsafe=true' to the " +
                $"command's meta-conditions. GET: {Settings._Uri}/help/topic=unsafe for more info."
        };

        internal static Response AmbiguousPutMatch() => new Response
        {
            StatusCode = (ushort) HttpStatusCode.Conflict,
            StatusDescription = "Found multiple entities matching the given conditions in a PUT command."
        };

        #endregion

        #region Success responses

        internal static Response NoContent() => new Response
        {
            StatusCode = (ushort) HttpStatusCode.NoContent,
            StatusDescription = "No results found matching query"
        };

        internal static Response InsertedEntities(int count, Type resource) => new Response
        {
            StatusCode = (ushort) HttpStatusCode.Created,
            StatusDescription = $"{count} entities inserted into resource '{resource.FullName}'"
        };

        internal static Response UpdatedEntities(int count, Type resource) => new Response
        {
            StatusCode = (ushort) HttpStatusCode.OK,
            StatusDescription = $"{count} entities updated in resource '{resource.FullName}'"
        };

        internal static Response DeleteEntities(int count, Type resource) => new Response
        {
            StatusCode = (ushort) HttpStatusCode.OK,
            StatusDescription = $"{count} entities deleted from resource '{resource.FullName}'"
        };

        internal static Response GetEntities(Command command, IEnumerable<dynamic> json)
        {
            var response = new Response();
            string jsonString;
            if (command.Select != null)
            {
                var list = new List<dynamic>();
                foreach (var o in json)
                {
                    var dict = new Dictionary<string, dynamic>();
                    var props = new List<PropertyInfo>();
                    foreach (var s in command.Select)
                    {
                        var matches = new List<PropertyInfo>();
                        foreach (var property in o.GetType().GetProperties())
                            if (s == property.Name.ToLower())
                                matches.Add(property);
                        if (matches.Count == 1)
                            props.Add(matches.First());
                        else if (matches.Count > 1)
                            throw new AmbiguousColumnException(command.Resource, s, matches.Select(m => m.Name).ToList());
                        else if (matches.Count < 1)
                            throw new UnknownColumnException(command.Resource, s);
                    }
                    foreach (var p in props)
                        dict[p.Name] = p.GetValue(o);
                    list.Add(dict);
                }
                jsonString = list.SerializeDyn();
            }
            else jsonString = json.Serialize(RESTarConfig.IEnumType[command.Resource]);
            
            if (command.OutputMimeType == RESTarMimeType.Excel)
            {
                var str = $@"{{""table"": {jsonString}}}";
                var dt = JsonConvert.DeserializeObject<DataSet>(str);
                var workbook = new XLWorkbook();
                workbook.AddWorksheet(dt);
                var fileName = $"{command.Resource.FullName}_export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                var path = $"{Application.Current.WorkingDirectory}/excel_exports";
                Directory.CreateDirectory(path);
                using (var memstream = new MemoryStream())
                {
                    workbook.SaveAs(memstream);
                    var bytes = memstream.ToArray();
                    using (var stream = File.Create($"{path}/{fileName}"))
                        stream.Write(bytes, 0, bytes.Length);
                    response.BodyBytes = bytes;
                }
                response.ContentType = "application/vnd.ms-excel";
                response.Headers["Content-Disposition"] = $"attachment; filename={fileName}";
            }

            return response;
        }

        #endregion
    }
}