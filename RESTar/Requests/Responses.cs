using System;
using System.Collections.Generic;
using System.Net;
using Starcounter;

namespace RESTar.Requests
{
    internal static class Responses
    {
        internal static Response AbortedOperation<T>(Exception e, IRequest<T> request) where T : class
        {
            return new Response
            {
                StatusCode = (ushort) HttpStatusCode.BadRequest,
                StatusDescription = "Bad request",
                Headers =
                {
                    ["RESTar-info"] = $"Aborted {request.Method} on resource '{request.Resource}' " +
                                      $"due to an error: {e.TotalMessage()}"
                }
            };
        }

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
            Headers = {["RESTar-info"] = "Error while deserializing JSON. Check JSON syntax."}
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

        internal static Response InternalError(Exception e) => new Response
        {
            StatusCode = (ushort) HttpStatusCode.InternalServerError,
            StatusDescription = "Internal server error",
            Headers = {["RESTar-info"] = e.Message}
        };

        internal static Response InfiniteLoop(Exception e) => new Response
        {
            StatusCode = 508,
            StatusDescription = "Infinite loop detected",
            Headers = {["RESTar-info"] = e.Message}
        };

        internal static Response RESTarInternalError(Exception e) => new Response
        {
            StatusCode = (ushort) HttpStatusCode.InternalServerError,
            StatusDescription = "Internal server error",
            Headers =
            {
                ["RESTar-info"] = $"Internal RESTar error: {e.Message}."
            }
        };


        internal static Response NoContent => new Response
        {
            StatusCode = (ushort) HttpStatusCode.NoContent,
            StatusDescription = "No content",
            Headers = {["RESTar-info"] = "No entities found matching request."}
        };

        internal static Response Forbidden(string message) => new Response
        {
            StatusCode = (ushort) HttpStatusCode.Forbidden,
            StatusDescription = "Forbidden",
            Headers = {["RESTar-info"] = message}
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