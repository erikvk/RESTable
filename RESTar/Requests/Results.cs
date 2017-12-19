using System;
using System.Collections.Generic;
using System.Net;
using RESTar.Operations;

namespace RESTar.Requests
{
    internal static class Results
    {
        internal static Result AbortedOperation<T>(Exception e, IRequest<T> request) where T : class => new Result(request)
        {
            StatusCode = HttpStatusCode.BadRequest,
            StatusDescription = "Bad request",
            Headers =
            {
                ["RESTar-info"] = $"Aborted {request.Method} on resource '{request.Resource}' " +
                                  $"due to an error: {e.TotalMessage()}"
            }
        };

        internal static Result AmbiguousResource(AmbiguousResourceException e) => new Result(null)
        {
            StatusCode = HttpStatusCode.NotFound,
            StatusDescription = "Not found",
            Headers = {["RESTar-info"] = e.Message}
        };

        internal static Result NotFound(Exception e) => new Result(null)
        {
            StatusCode = HttpStatusCode.NotFound,
            StatusDescription = "Not found",
            Headers = {["RESTar-info"] = e.Message}
        };

        internal static Result AmbiguousProperty(AmbiguousPropertyException e) => new Result(null)
        {
            StatusCode = HttpStatusCode.NotFound,
            StatusDescription = "Not found",
            Headers =
            {
                ["RESTar-info"] = $"{e.Message}Try qualifying the property name further, e.g. from " +
                                  $"'{e.SearchString}' to '{e.Candidates[0]}'."
            }
        };

        internal static Result BadRequest(Exception e) => new Result(null)
        {
            StatusCode = HttpStatusCode.BadRequest,
            StatusDescription = "Bad request",
            Headers = {["RESTar-info"] = e.Message}
        };

        internal static Result UnknownHandlerAction => new Result(null)
        {
            StatusCode = HttpStatusCode.BadRequest,
            StatusDescription = "Bad request",
            Headers = {["RESTar-info"] = "Unknown RESTar handler action"}
        };

        internal static Result JsonError => new Result(null)
        {
            StatusCode = HttpStatusCode.BadRequest,
            StatusDescription = "Bad request",
            Headers = {["RESTar-info"] = "Error while deserializing JSON. Check JSON syntax."}
        };

        internal static Result DbError(Exception e)
        {
            if (e.Message.Contains("SCERR4034"))
                return new Result(null)
                {
                    StatusCode = HttpStatusCode.Forbidden,
                    StatusDescription = "Forbidden",
                    Headers =
                    {
                        ["RESTar-info"] = "The operation was aborted by a commit hook. " +
                                          (e.InnerException?.Message ?? e.Message)
                    }
                };
            return new Result(null)
            {
                StatusCode = HttpStatusCode.InternalServerError,
                StatusDescription = "Internal server error",
                Headers =
                {
                    ["RESTar-info"] = "The Starcounter database encountered an error: " +
                                      (e.InnerException?.Message ?? e.Message)
                }
            };
        }

        internal static Result InternalError(Exception e) => new Result(null)
        {
            StatusCode = HttpStatusCode.InternalServerError,
            StatusDescription = "Internal server error",
            Headers = {["RESTar-info"] = e.Message}
        };

        internal static Result InfiniteLoop(Exception e) => new Result(null)
        {
            StatusCode = (HttpStatusCode) 508,
            StatusDescription = "Infinite loop detected",
            Headers = {["RESTar-info"] = e.Message}
        };

        internal static Result RESTarInternalError(Exception e) => new Result(null)
        {
            StatusCode = HttpStatusCode.InternalServerError,
            StatusDescription = "Internal server error",
            Headers = {["RESTar-info"] = $"Internal RESTar error: {e.Message}."}
        };


        internal static Result NoContent => new Result(null)
        {
            StatusCode = HttpStatusCode.NoContent,
            StatusDescription = "No content",
            Headers = {["RESTar-info"] = "No entities found matching request."}
        };

        internal static Result Forbidden(string message) => new Result(null)
        {
            StatusCode = HttpStatusCode.Forbidden,
            StatusDescription = "Forbidden",
            Headers = {["RESTar-info"] = message}
        };

        internal static Result AllowOrigin(string allowedOrigin, IEnumerable<Methods> allowedMethods) => new Result(null)
        {
            StatusCode = HttpStatusCode.OK,
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