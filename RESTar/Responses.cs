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
            StatusDescription = "Not found",
            Headers =
            {
                ["RESTar-info"] = $"{e.Message}Try qualifying the resource name further, e.g. from " +
                                  $"'{e.SearchString}' to '{e.Candidates.First()}'."
            }
        };

        internal static Response NotFound(Exception e) => new Response
        {
            StatusCode = (ushort) HttpStatusCode.NotFound,
            StatusDescription = "Not found",
            Headers =
            {
                ["RESTar-info"] = e.Message
            }
        };

        internal static Response AmbiguousColumn(AmbiguousColumnException e) => new Response
        {
            StatusCode = (ushort) HttpStatusCode.NotFound,
            StatusDescription = "Not found",
            Headers =
            {
                ["RESTar-info"] = $"{e.Message}Try qualifying the column name further, e.g. from " +
                                  $"'{e.SearchString}' to '{e.Candidates.First()}'."
            }
        };

        #endregion

        #region Bad request

        internal static Response AbortedOperation(Exception e, RESTarMethods method, Type resource)
        {
            var alias = ResourceAlias.ByResource(resource);
            return new Response
            {
                StatusCode = (ushort) HttpStatusCode.BadRequest,
                StatusDescription = "Bad request",
                Headers =
                {
                    ["RESTar-info"] = $"Aborted {method} on resource '{resource.FullName}'" +
                                      $"{(alias != null ? $" ('{alias}')" : "")} due to an error: {e.TotalMessage()}"
                }
            };
        }

        internal static Response BadRequest(Exception e) => new Response
        {
            StatusCode = (ushort) HttpStatusCode.BadRequest,
            StatusDescription = "Bad request",
            Headers =
            {
                ["RESTar-info"] = e.Message
            }
        };

        internal static Response SemanticsError(Exception e) => new Response
        {
            StatusCode = (ushort) HttpStatusCode.BadRequest,
            StatusDescription = "Bad request",
            Headers =
            {
                ["RESTar-info"] = $"{e.Message}To enumerate columns in a resource R: GET " +
                                  $"{Settings._ResourcesPath}/RESTar.resource/name=R"
            }
        };

        internal static Response DeserializationError(string json) => new Response
        {
            StatusCode = (ushort) HttpStatusCode.BadRequest,
            StatusDescription = "Bad request",
            Headers =
            {
                ["RESTar-info"] = $"Error while deserializing JSON. Check JSON syntax:\n{json}"
            }
        };

        internal static Response DatabaseError(Exception e)
        {
            if (e.Message.Contains("SCERR4034"))
                return new Response
                {
                    StatusCode = (ushort) HttpStatusCode.Forbidden,
                    StatusDescription = "Forbidden",
                    Headers =
                    {
                        ["RESTar-info"] = "The operation was aborted by a commit hook. " +
                                          (e.InnerException?.Message ?? e.Message)
                    }
                };
            return new Response
            {
                StatusCode = (ushort) HttpStatusCode.InternalServerError,
                StatusDescription = "Internal server error",
                Headers =
                {
                    ["RESTar-info"] = "The Starcounter database encountered an error: " +
                                      (e.InnerException?.Message ?? e.Message)
                }
            };
        }

        #endregion

        #region Internal

        internal static Response InternalError(Exception e) => new Response
        {
            StatusCode = (ushort) HttpStatusCode.InternalServerError,
            StatusDescription = "Internal server error",
            Headers =
            {
                ["RESTar-info"] = $"Internal error: {e.Message} " +
                                  $"{e.InnerException?.Message} " +
                                  $"{e.InnerException?.InnerException?.Message}"
            }
        };

        internal static Response RESTarInternalError(Exception e) => new Response
        {
            StatusCode = (ushort) HttpStatusCode.InternalServerError,
            StatusDescription = "Internal server error",
            Headers =
            {
                ["RESTar-info"] = $"Internal RESTar error: {e.Message}"
            }
        };

        #endregion

        #region Success responses

        internal static Response NoContent() => new Response
        {
            StatusCode = (ushort) HttpStatusCode.NoContent,
            StatusDescription = "No content",
            Headers =
            {
                ["RESTar-info"] = "No entities found matching request"
            }
        };

        internal static Response InsertedEntities(Request request, int count, Type resource)
        {
            var alias = ResourceAlias.ByResource(resource);
            return new Response
            {
                StatusCode = (ushort) HttpStatusCode.Created,
                StatusDescription = "Created",
                Headers =
                {
                    ["RESTar-info"] = $"{count} entities inserted into resource '{resource.FullName}'" +
                                      $"{(alias != null ? $" ('{alias}')" : "")}"
                }
            };
        }

        internal static Response UpdatedEntities(Request request, int count, Type resource)
        {
            var alias = ResourceAlias.ByResource(resource);
            return new Response
            {
                StatusCode = (ushort) HttpStatusCode.OK,
                StatusDescription = "OK",
                Headers =
                {
                    ["RESTar-info"] = $"{count} entities updated in resource '{resource.FullName}'" +
                                      $"{(alias != null ? $" ('{alias}')" : "")}"
                }
            };
        }

        internal static Response SafePostedEntities(Request request, int insertedCount, int updatedCount)
        {
            return new Response
            {
                StatusCode = 200,
                Headers =
                {
                    ["RESTar-info"] = $"Inserted {insertedCount} and updated {updatedCount} entities " +
                                      $"in resource {request.Resource.Name}"
                }
            };
        }

        internal static Response DeleteEntities(int count, Type resource)
        {
            var alias = ResourceAlias.ByResource(resource);
            return new Response
            {
                StatusCode = (ushort) HttpStatusCode.OK,
                StatusDescription = "OK",
                Headers =
                {
                    ["RESTar-info"] = $"{count} entities deleted from resource '{resource.FullName}'" +
                                      $"{(alias != null ? $" ('{alias}')" : "")}"
                }
            };
        }

        internal static Response GetEntities(Request request, IEnumerable<dynamic> entities)
        {
            var response = new Response();
            if (request.Accept != RESTarMimeType.Excel)
            {
                string jsonString;
                if
                (
                    request.Dynamic ||
                    request.Select != null ||
                    request.Rename != null ||
                    request.Resource.TargetType.IsSubclassOf(typeof(DDictionary)) ||
                    request.Resource.TargetType.GetAttribute<RESTarAttribute>()?.Dynamic == true
                )
                {
                    jsonString = entities.SerializeDyn();
                }
                else if (request.Map != null)
                    jsonString = entities.Serialize(typeof(IEnumerable<>)
                        .MakeGenericType(typeof(Dictionary<,>)
                            .MakeGenericType(typeof(string), request.Resource.TargetType)));
                else jsonString = entities.Serialize(RESTarConfig.IEnumTypes[request.Resource]);
                response.ContentType = MimeTypes.JSON;
                response.Body = jsonString;
            }
            else
            {
                var data = ToDataSet(entities);
                var workbook = new XLWorkbook();
                workbook.AddWorksheet(data);
                var fileName = $"{request.Resource.Name}_export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                using (var memstream = new MemoryStream())
                {
                    workbook.SaveAs(memstream);
                    response.BodyBytes = memstream.ToArray();
                }
                response.ContentType = MimeTypes.Excel;
                response.Headers["Content-Disposition"] = $"attachment; filename={fileName}";
            }
            return response;
        }

        private static DataSet ToDataSet(this IEnumerable<dynamic> list)
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
                            if (!t.Columns.Contains(pair.Key))
                                t.Columns.Add(pair.Key);
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

        internal static Response Forbidden() => new Response
        {
            StatusCode = (ushort) HttpStatusCode.Forbidden,
            StatusDescription = "Forbidden"
        };

        internal static Response AllowOrigin(string allowedOrigin, IEnumerable<RESTarMethods> allowedMethods) =>
            new Response
            {
                StatusCode = (ushort) HttpStatusCode.OK,
                StatusDescription = "OK",
                Headers =
                {
                    ["Access-Control-Allow-Origin"] = RESTarConfig.AllowAllOrigins ? "*" : allowedOrigin,
                    ["Access-Control-Allow-Methods"] = string.Join(", ", ToExternalMethodsList(allowedMethods)),
                    ["Access-Control-Max-Age"] = "120",
                    ["Access-Control-Allow-Credentials"] = "true",
                    ["Access-Control-Allow-Headers"] = "origin, content-type, accept, authorization, " +
                                                       "source, destination"
                }
            };

        private static IEnumerable<RESTarMethods> ToExternalMethodsList(IEnumerable<RESTarMethods> methods)
        {
            return methods.Select(i =>
            {
                var str = i.ToString();
                if (str.Contains("ADMIN"))
                    return (RESTarMethods) Enum.Parse(typeof(RESTarMethods), str.Split('_')[1]);
                return i;
            }).Distinct();
        }
    }
}