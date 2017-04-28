using System;
using Newtonsoft.Json;
using RESTar.Operations;
using Starcounter;
using ScRequest = Starcounter.Request;
using ScHandle = Starcounter.Handle;

namespace RESTar.Requests
{
    internal static class Handlers
    {
        internal static void Register(string uri)
        {
            uri += "{?}";
            ScHandle.GET(Settings._Port, uri, (ScRequest r, string q) => Handle(r, q, Evaluators.GET, RESTarMethods.GET));
            ScHandle.POST(Settings._Port, uri, (ScRequest r, string q) => Handle(r, q, Evaluators.POST, RESTarMethods.POST));
            ScHandle.PUT(Settings._Port, uri, (ScRequest r, string q) => Handle(r, q, Evaluators.PUT, RESTarMethods.PUT));
            ScHandle.PATCH(Settings._Port, uri, (ScRequest r, string q) => Handle(r, q, Evaluators.PATCH, RESTarMethods.PATCH));
            ScHandle.DELETE(Settings._Port, uri, (ScRequest r, string q) => Handle(r, q, Evaluators.DELETE, RESTarMethods.DELETE));
            ScHandle.OPTIONS(Settings._Port, uri, (ScRequest r, string q) => CheckOrigin(r, q));
        }

        private static Response CheckOrigin(ScRequest scRequest, string query)
        {
            try
            {
                var request = new Request(scRequest);
                request.Populate(query, default(RESTarMethods), null);
                var origin = request.ScRequest.Headers["Origin"].ToLower();
                if (RESTarConfig.AllowAllOrigins || RESTarConfig.AllowedOrigins.Contains(new Uri(origin)))
                    return Responses.AllowOrigin(origin, request.Resource.AvailableMethods);
                return Responses.Forbidden();
            }
            catch
            {
                return Responses.Forbidden();
            }
        }

        private static Response Handle(ScRequest scRequest, string query, Func<Request, Response> evaluator,
            RESTarMethods method)
        {
            Request request = null;
            try
            {
                using (request = new Request(scRequest))
                {
                    request.Authenticate();
                    request.Populate(query, method, evaluator);
                    request.MethodCheck();
                    request.GetRequestData();
                    request.Evaluate();
                    request.Transaction.Commit();
                    return request.GetResponse();
                }
            }

            #region Catch exceptions

            catch (Exception e)
            {
                request?.Transaction.Rollback();

                Response errorResponse;
                ErrorCode errorCode;

                if (e is ForbiddenException) return Responses.Forbidden();
                if (e is NoContentException) return Responses.NoContent();

                if (e is SyntaxException)
                {
                    errorCode = ((SyntaxException) e).errorCode;
                    errorResponse = Responses.BadRequest(e);
                }
                else if (e is FormatException)
                {
                    errorCode = ErrorCode.UnsupportedContentType;
                    errorResponse = Responses.BadRequest(e);
                }
                else if (e is UnknownColumnException)
                {
                    errorCode = ErrorCode.UnknownColumnError;
                    errorResponse = Responses.NotFound(e);
                }
                else if (e is CustomEntityUnknownColumnException)
                {
                    errorCode = ErrorCode.UnknownColumnInGeneratedObjectError;
                    errorResponse = Responses.NotFound(e);
                }
                else if (e is AmbiguousColumnException)
                {
                    errorCode = ErrorCode.AmbiguousColumnError;
                    errorResponse = Responses.AmbiguousColumn((AmbiguousColumnException) e);
                }
                else if (e is SourceException)
                {
                    errorCode = ErrorCode.InvalidSourceDataError;
                    errorResponse = Responses.BadRequest(e);
                }
                else if (e is UnknownResourceException)
                {
                    errorCode = ErrorCode.UnknownResourceError;
                    errorResponse = Responses.NotFound(e);
                }
                else if (e is AmbiguousResourceException)
                {
                    errorCode = ErrorCode.AmbiguousResourceError;
                    errorResponse = Responses.AmbiguousResource((AmbiguousResourceException) e);
                }
                else if (e is InvalidInputCountException)
                {
                    errorCode = ErrorCode.DataSourceFormatError;
                    errorResponse = Responses.BadRequest(e);
                }
                else if (e is ExcelInputException)
                {
                    errorCode = ErrorCode.ExcelReaderError;
                    errorResponse = Responses.BadRequest(e);
                }
                else if (e is ExcelFormatException)
                {
                    errorCode = ErrorCode.ExcelReaderError;
                    errorResponse = Responses.BadRequest(e);
                }
                else if (e is JsonReaderException)
                {
                    errorCode = ErrorCode.JsonDeserializationError;
                    errorResponse = Responses.DeserializationError(scRequest.Body);
                }
                else if (e is DbException)
                {
                    errorCode = ErrorCode.DatabaseError;
                    errorResponse = Responses.DatabaseError(e);
                }
                else if (e is AbortedSelectorException)
                {
                    errorCode = ErrorCode.AbortedOperation;
                    errorResponse = Responses.AbortedOperation(e, method, request?.Resource.TargetType);
                }
                else if (e is AbortedInserterException)
                {
                    errorCode = ErrorCode.AbortedOperation;
                    errorResponse = Responses.AbortedOperation(e, method, request?.Resource.TargetType);
                }
                else if (e is AbortedUpdaterException)
                {
                    errorCode = ErrorCode.AbortedOperation;
                    errorResponse = Responses.AbortedOperation(e, method, request?.Resource.TargetType);
                }
                else if (e is AbortedDeleterException)
                {
                    errorCode = ErrorCode.AbortedOperation;
                    errorResponse = Responses.AbortedOperation(e, method, request?.Resource.TargetType);
                }
                else
                {
                    errorCode = ErrorCode.UnknownError;
                    errorResponse = Responses.InternalError(e);
                }

                Error error = null;
                Error.ClearOld();
                Db.TransactAsync(() => error = new Error(errorCode, e, request));
                errorResponse.Headers["ErrorInfo"] = $"{Settings._Uri}/{typeof(Error).FullName}/id={error.Id}";
                return errorResponse;
            }

            #endregion
        }
    }
}