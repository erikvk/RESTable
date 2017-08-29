using System;
using System.Collections.Generic;
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
            Headers = {["RESTar-info"] = e.Message}
        };

        internal static Response NotFound(Exception e) => new Response
        {
            StatusCode = (ushort) HttpStatusCode.NotFound,
            StatusDescription = "Not found",
            Headers = {["RESTar-info"] = e.Message}
        };

        internal static Response AmbiguousProperty(AmbiguousPropertyException e) => new Response
        {
            StatusCode = (ushort) HttpStatusCode.NotFound,
            StatusDescription = "Not found",
            Headers =
            {
                ["RESTar-info"] = $"{e.Message}Try qualifying the property name further, e.g. from " +
                                  $"'{e.SearchString}' to '{e.Candidates[0]}'."
            }
        };

        #endregion

        #region Bad request

        internal static Response AbortedOperation<T>(Exception e, Methods method) where T : class
        {
            return new Response
            {
                StatusCode = (ushort) HttpStatusCode.BadRequest,
                StatusDescription = "Bad request",
                Headers =
                {
                    ["RESTar-info"] = $"Aborted {method} on resource '{typeof(T).FullName}' " +
                                      $"due to an error: {e.TotalMessage()}"
                }
            };
        }

        internal static Response BadRequest(Exception e) => new Response
        {
            StatusCode = (ushort) HttpStatusCode.BadRequest,
            StatusDescription = "Bad request",
            Headers = {["RESTar-info"] = e.Message}
        };

        internal static Response UnknownHandlerAction => new Response
        {
            StatusCode = (ushort) HttpStatusCode.BadRequest,
            StatusDescription = "Bad request",
            Headers = {["RESTar-info"] = "Unknown RESTar handler action"}
        };

        internal static Response JsonError => new Response
        {
            StatusCode = (ushort) HttpStatusCode.BadRequest,
            StatusDescription = "Bad request",
            Headers = {["RESTar-info"] = "Error while deserializing JSON. Check JSON syntax"}
        };

        internal static Response DbError(Exception e)
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

        internal static Response NoContent => new Response
        {
            StatusCode = (ushort) HttpStatusCode.NoContent,
            StatusDescription = "No content",
            Headers = {["RESTar-info"] = "No entities found matching request"}
        };

        internal static Response InsertedEntities<T>(int count) where T : class => new Response
        {
            StatusCode = (ushort) HttpStatusCode.Created,
            StatusDescription = "Created",
            Headers = {["RESTar-info"] = $"{count} entities inserted into resource '{typeof(T).FullName}'"}
        };

        internal static Response UpdatedEntities<T>(int count) where T : class => new Response
        {
            StatusCode = (ushort) HttpStatusCode.OK,
            StatusDescription = "OK",
            Headers = {["RESTar-info"] = $"{count} entities updated in resource '{typeof(T).FullName}'"}
        };

        internal static Response SafePostedEntities<T>(int ins, int upd) where T : class => new Response
        {
            StatusCode = 200,
            Headers = {["RESTar-info"] = $"Inserted {ins} and updated {upd} entities in resource {typeof(T).FullName}"}
        };

        internal static Response DeletedEntities<T>(int count) where T : class => new Response
        {
            StatusCode = (ushort) HttpStatusCode.OK,
            StatusDescription = "OK",
            Headers = {["RESTar-info"] = $"{count} entities deleted from resource '{typeof(T).FullName}'"}
        };

        #endregion

        internal static Response Forbidden => new Response
        {
            StatusCode = (ushort) HttpStatusCode.Forbidden,
            StatusDescription = "Forbidden"
        };

        internal static Response AllowOrigin(string allowedOrigin, IEnumerable<Methods> allowedMethods) => new Response
        {
            StatusCode = (ushort) HttpStatusCode.OK,
            StatusDescription = "OK",
            Headers =
            {
                ["Access-Control-Allow-Origin"] = RESTarConfig.AllowAllOrigins ? "*" : allowedOrigin,
                ["Access-Control-Allow-Methods"] = string.Join(", ", allowedMethods),
                ["Access-Control-Max-Age"] = "120",
                ["Access-Control-Allow-Credentials"] = "true",
                ["Access-Control-Allow-Headers"] = "origin, content-type, accept, authorization, " +
                                                   "source, destination"
            }
        };
    }
}