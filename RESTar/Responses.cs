using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Starcounter;
using System.Data;
using System.IO;
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
            StatusDescription = $"Error while deserializing JSON. Check JSON syntax:\n{json}"
        };

        internal static Response BlockedMethod(RESTarMethods method, Type resource) => new Response
        {
            StatusCode = (ushort) HttpStatusCode.Forbidden,
            StatusDescription = $"{method} is blocked for resource '{resource.FullName}'. Available " +
                                $"methods: {resource.AvailableMethods()?.ToMethodsString()}"
        };

        internal static Response ExternalSourceError(ExternalSourceException e) => new Response
        {
            StatusCode = (ushort) HttpStatusCode.BadRequest,
            StatusDescription = e.Message
        };

        internal static Response InvalidInputCount(InvalidInputCountException e) => new Response
        {
            StatusCode = (ushort) HttpStatusCode.BadRequest,
            StatusDescription = e.Message
        };

        internal static Response ExcelFormatError(Exception e) => new Response
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
                $"command's meta-conditions. See help article with topic 'unsafe' for more info."
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

        internal static Response GetEntities(Command command, IEnumerable<dynamic> entities)
        {
            var response = new Response();
            string jsonString;

            #region Only Select

            if (command.Select != null && command.Rename == null)
            {
                var columns = command.Resource.GetColumns();
                var newEntitiesList = new List<dynamic>();
                foreach (var entity in entities)
                {
                    var newEntity = new Dictionary<string, dynamic>();
                    foreach (var s in command.Select)
                    {
                        var column = columns.FindColumn(command.Resource, s);
                        newEntity[column.GetColumnName()] = column.GetValue(entity);
                    }
                    newEntitiesList.Add(newEntity);
                }
                jsonString = newEntitiesList.SerializeDyn();
            }

            #endregion

            #region Only Rename

            else if (command.Select == null && command.Rename != null)
            {
                var columns = command.Resource.GetColumns();
                var newEntitiesList = new List<dynamic>();
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
                jsonString = newEntitiesList.SerializeDyn();
            }

            #endregion

            #region Both Select and Rename

            else if (command.Select != null && command.Rename != null)
            {
                var columns = command.Resource.GetColumns();
                var newEntitiesList = new List<dynamic>();
                foreach (var entity in entities)
                {
                    var newEntity = new Dictionary<string, dynamic>();
                    foreach (var s in command.Select)
                    {
                        var column = columns.FindColumn(command.Resource, s);
                        string newKey;
                        command.Rename.TryGetValue(s, out newKey);
                        newEntity[newKey ?? column.GetColumnName()] = column.GetValue(entity);
                    }
                    newEntitiesList.Add(newEntity);
                }
                jsonString = newEntitiesList.SerializeDyn();
            }

            #endregion

            else if (command.Dynamic)
                jsonString = entities.SerializeDyn();
            else jsonString = entities.Serialize(RESTarConfig.IEnumTypes[command.Resource]);

            if (command.OutputMimeType == RESTarMimeType.Excel)
            {
                var str = $@"{{""table"": {jsonString}}}";
                DataSet data;
                try
                {
                    data = JsonConvert.DeserializeObject<DataSet>(str, new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Include,
                        DateFormatHandling = DateFormatHandling.IsoDateFormat
                    });
                }
                catch (Exception)
                {
                    throw new ExcelFormatException();
                }
                var workbook = new XLWorkbook();
                workbook.AddWorksheet(data);
                var fileName = $"{command.Resource.FullName}_export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                using (var memstream = new MemoryStream())
                {
                    workbook.SaveAs(memstream);
                    response.BodyBytes = memstream.ToArray();
                }
                response.ContentType = "application/vnd.ms-excel";
                response.Headers["Content-Disposition"] = $"attachment; filename={fileName}";
            }
            else
            {
                response.ContentType = "application/json";
                response.Body = jsonString;
            }

            return response;
        }

        #endregion
    }
}