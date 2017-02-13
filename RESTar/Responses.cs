using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using ClosedXML.Excel;
using Dynamit;
using Starcounter;

namespace RESTar
{
    internal static class Responses
    {
        #region Not found

        internal static Response AmbiguousResource(AmbiguousResourceException e) => new Response
        {
            StatusCode = (ushort) HttpStatusCode.NotFound,
            StatusDescription = e.Message + "Try qualifying the resource name further, e.g. from " +
                                $"'{e.SearchString}' to '{e.Candidates.First()}'."
        };

        internal static Response NotFound(Exception e) => new Response
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

        #endregion

        #region Bad request

        internal static Response AbortedOperation(Exception e, RESTarMethods method, Type resource) => new Response
        {
            StatusCode = (ushort) HttpStatusCode.BadRequest,
            StatusDescription = $"Aborted {method} on resource '{resource.FullName}' due to an error: {e.Message}"
        };

        internal static Response BadRequest(Exception e) => new Response
        {
            StatusCode = (ushort) HttpStatusCode.BadRequest,
            StatusDescription = e.Message
        };

        internal static Response SemanticsError(Exception e) => new Response
        {
            StatusCode = (ushort) HttpStatusCode.BadRequest,
            StatusDescription = $"{e.Message}To enumerate columns in a resource R: GET " +
                                $"{Settings._ResourcesPath}/RESTar.resource/name=R"
        };

        internal static Response DeserializationError(string json) => new Response
        {
            StatusCode = (ushort) HttpStatusCode.BadRequest,
            StatusDescription = $"Error while deserializing JSON. Check JSON syntax:\n{json}"
        };

        internal static Response DatabaseError(Exception e)
        {
            if (e.Message.Contains("SCERR4034"))
                return new Response
                {
                    StatusCode = (ushort) HttpStatusCode.Forbidden,
                    StatusDescription = "The operation was aborted by a commit hook. " +
                                        (e.InnerException?.Message ?? e.Message)
                };

            return new Response
            {
                StatusCode = (ushort) HttpStatusCode.InternalServerError,
                StatusDescription = "The Starcounter database encountered an error: " +
                                    (e.InnerException?.Message ?? e.Message)
            };
        }

        internal static Response BlockedMethod(RESTarMethods method, Type resource) => new Response
        {
            StatusCode = (ushort) HttpStatusCode.Forbidden,
            StatusDescription = $"{method} is blocked for resource '{resource.FullName}'. Available " +
                                $"methods: {resource.AvailableMethods()?.ToMethodsString()}"
        };

        #endregion

        #region Internal

        internal static Response InternalError(Exception e) => new Response
        {
            StatusCode = (ushort) HttpStatusCode.InternalServerError,
            StatusDescription = $"Internal error: {e.Message} " +
                                $"{e.InnerException?.Message} " +
                                $"{e.InnerException?.InnerException?.Message}"
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
                $"Expected a uniquely matched entity in resource '{resource.FullName}' for this request, " +
                "but matched multiple entities satisfying the given conditions. To enable manipulation of " +
                "multiple matched entities (for methods that support this), add 'unsafe=true' to the " +
                $"request's meta-conditions. See help article with topic 'unsafe' for more info."
        };

        internal static Response AmbiguousPutMatch() => new Response
        {
            StatusCode = (ushort) HttpStatusCode.Conflict,
            StatusDescription = "Found multiple entities matching the given conditions in a PUT request."
        };

        #endregion

        #region Success responses

        internal static Response NoContent() => new Response
        {
            StatusCode = (ushort) HttpStatusCode.NoContent,
            StatusDescription = "No results found matching query"
        };

        internal static Response InsertedEntities(Request request, int count, Type resource)
        {
            if (request.Imgput != null)
                return new Response
                {
                    StatusCode = (ushort) HttpStatusCode.Created,
                    Body = "R0lGODlhAQABAIAAAP///wAAACwAAAAAAQABAAACAkQBADs=",
                    ContentType = "image/gif"
                };
            return new Response
            {
                StatusCode = (ushort) HttpStatusCode.Created,
                StatusDescription = $"{count} entities inserted into resource '{resource.FullName}'"
            };
        }

        internal static Response UpdatedEntities(Request request, int count, Type resource)
        {
            if (request.Imgput != null)
                return new Response
                {
                    StatusCode = (ushort) HttpStatusCode.OK,
                    Body = "R0lGODlhAQABAIAAAP///wAAACwAAAAAAQABAAACAkQBADs=",
                    ContentType = "image/gif"
                };
            return new Response
            {
                StatusCode = (ushort) HttpStatusCode.OK,
                StatusDescription = $"{count} entities updated in resource '{resource.FullName}'"
            };
        }

        internal static Response DeleteEntities(int count, Type resource) => new Response
        {
            StatusCode = (ushort) HttpStatusCode.OK,
            StatusDescription = $"{count} entities deleted from resource '{resource.FullName}'"
        };

        internal static Response GetEntities(Request request, IEnumerable<dynamic> entities)
        {
            Response response;
            if (request.OutputMimeType != RESTarMimeType.Excel)
            {
                string jsonString;
                response = new Response();
                if
                (
                    request.Dynamic ||
                    request.Select != null ||
                    request.Rename != null ||
                    request.Resource.IsSubclassOf(typeof(DDictionary)) ||
                    request.Resource.GetAttribute<RESTarAttribute>().Dynamic
                )
                {
                    jsonString = entities.SerializeDyn();
                }
                else if (request.Map != null)
                    jsonString = entities.Serialize(typeof(IEnumerable<>)
                        .MakeGenericType(typeof(Dictionary<,>)
                            .MakeGenericType(typeof(string), request.Resource)));
                else jsonString = entities.Serialize(RESTarConfig.IEnumTypes[request.Resource]);
                response.ContentType = "application/json";
                response.Body = jsonString;
            }
            else
            {
                response = new Response();
                var data = ToDataSet(entities);
                var workbook = new XLWorkbook();
                workbook.AddWorksheet(data);
                var fileName = $"{request.Resource.FullName}_export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                using (var memstream = new MemoryStream())
                {
                    workbook.SaveAs(memstream);
                    response.BodyBytes = memstream.ToArray();
                }
                response.ContentType = "application/vnd.ms-excel";
                response.Headers["Content-Disposition"] = $"attachment; filename={fileName}";
            }
            return response;
        }

        public static DataSet ToDataSet(this IEnumerable<dynamic> list)
        {
            var ds = new DataSet();
            var t = new DataTable();
            ds.Tables.Add(t);

            var first = list.First();
            if (first is IDictionary<string, dynamic>)
            {
                foreach (var item in list)
                {
                    var row = t.NewRow();
                    foreach (var pair in item)
                    {
                        try
                        {
                            row[pair.Key] = pair.Value ?? DBNull.Value;
                        }
                        catch
                        {
                            try
                            {
                                row[pair.Key] = DbHelper.GetObjectNo(pair.Value) ?? DBNull.Value;
                            }
                            catch
                            {
                                row[pair.Key] = pair.Value?.ToString() ?? DBNull.Value;
                            }
                        }
                    }
                    t.Rows.Add(row);
                }
            }
            else
            {
                Type elementType = first.GetType();
                foreach (var propInfo in elementType.GetColumns())
                {
                    var ColType = propInfo.PropertyType.IsClass && propInfo.PropertyType != typeof(string)
                        ? typeof(string)
                        : Nullable.GetUnderlyingType(propInfo.PropertyType) ?? propInfo.PropertyType;
                    t.Columns.Add(propInfo.Name, ColType);
                }
                foreach (var item in list)
                {
                    var row = t.NewRow();
                    foreach (var propInfo in elementType.GetColumns())
                    {
                        var value = propInfo.GetValue(item, null);
                        try
                        {
                            row[propInfo.Name] = propInfo.HasAttribute<ExcelFlattenToString>()
                                ? value.ToString()
                                : "$(ObjectID: " + DbHelper.GetObjectID(value) + ")";
                        }
                        catch
                        {
                            row[propInfo.Name] = value ?? DBNull.Value;
                        }
                    }
                    t.Rows.Add(row);
                }
            }
            return ds;
        }

        #endregion
    }
}