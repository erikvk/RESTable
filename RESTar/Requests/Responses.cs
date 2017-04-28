using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Starcounter;

namespace RESTar.Requests
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
            request.SetResponseData(entities, response);
            return response;
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
                })
                .Distinct();
        }
    }
}