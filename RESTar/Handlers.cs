using System;
using Jil;
using Newtonsoft.Json;
using Starcounter;
using static RESTar.ErrorCode;
using static RESTar.Responses;
using static RESTar.RESTarConfig;
using static RESTar.RESTarMethods;
using static RESTar.Settings;
using ScRequest = Starcounter.Request;

namespace RESTar
{
    internal static class Handlers
    {
        internal static void Register(string uri)
        {
            uri += "{?}";
            Handle.GET(_Port, uri, (ScRequest r, string q) => Evaluate(r, q, Evaluators.GET, GET));
            Handle.POST(_Port, uri, (ScRequest r, string q) => Evaluate(r, q, Evaluators.POST, POST));
            Handle.PUT(_Port, uri, (ScRequest r, string q) => Evaluate(r, q, Evaluators.PUT, PUT));
            Handle.PATCH(_Port, uri, (ScRequest r, string q) => Evaluate(r, q, Evaluators.PATCH, PATCH));
            Handle.DELETE(_Port, uri, (ScRequest r, string q) => Evaluate(r, q, Evaluators.DELETE, DELETE));
            Handle.OPTIONS(_Port, uri, (ScRequest r, string q) => CheckOrigin(r, q));
        }

        private static Response CheckOrigin(ScRequest scRequest, string query)
        {
            try
            {
                var request = new Request(scRequest);
                request.Populate(query, default(RESTarMethods), null);
                var origin = request.ScRequest.Headers["Origin"].ToLower();
                if (AllowAllOrigins || AllowedOrigins.Contains(new Uri(origin)))
                    return AllowOrigin(origin, request.Resource.AvailableMethods);
                return Forbidden();
            }
            catch
            {
                return Forbidden();
            }
        }

        private static Response Evaluate(ScRequest scRequest, string query, Func<Request, Response> evaluator,
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
                    request.ResolveDataSource();
                    var response = request.Evaluate();
                    return request.Respond(response);
                }
            }

            #region Catch exceptions

            catch (Exception e)
            {
                Response errorResponse;
                ErrorCode errorCode;

                if (e is ForbiddenException) return Forbidden();
                if (e is NoContentException) return NoContent();

                if (e is SyntaxException)
                {
                    errorCode = ((SyntaxException) e).errorCode;
                    errorResponse = BadRequest(e);
                }
                else if (e is UnknownColumnException)
                {
                    errorCode = UnknownColumnError;
                    errorResponse = NotFound(e);
                }
                else if (e is CustomEntityUnknownColumnException)
                {
                    errorCode = UnknownColumnInGeneratedObjectError;
                    errorResponse = NotFound(e);
                }
                else if (e is AmbiguousColumnException)
                {
                    errorCode = AmbiguousColumnError;
                    errorResponse = AmbiguousColumn((AmbiguousColumnException) e);
                }
                else if (e is SourceException)
                {
                    errorCode = InvalidSourceDataError;
                    errorResponse = BadRequest(e);
                }
                else if (e is UnknownResourceException)
                {
                    errorCode = UnknownResourceError;
                    errorResponse = NotFound(e);
                }
                else if (e is AmbiguousResourceException)
                {
                    errorCode = AmbiguousResourceError;
                    errorResponse = AmbiguousResource((AmbiguousResourceException) e);
                }
                else if (e is InvalidInputCountException)
                {
                    errorCode = DataSourceFormatError;
                    errorResponse = BadRequest(e);
                }
                else if (e is ExcelInputException)
                {
                    errorCode = ExcelReaderError;
                    errorResponse = BadRequest(e);
                }
                else if (e is ExcelFormatException)
                {
                    errorCode = ExcelReaderError;
                    errorResponse = BadRequest(e);
                }
                else if (e is JsonReaderException)
                {
                    errorCode = JsonDeserializationError;
                    errorResponse = DeserializationError(scRequest.Body);
                }
                else if (e is DbException)
                {
                    errorCode = ErrorCode.DatabaseError;
                    errorResponse = DatabaseError(e);
                }
                else if (e is AbortedSelectorException)
                {
                    errorCode = ErrorCode.AbortedOperation;
                    errorResponse = AbortedOperation(e, method, request?.Resource.TargetType);
                }
                else if (e is AbortedInserterException)
                {
                    errorCode = ErrorCode.AbortedOperation;
                    errorResponse = AbortedOperation(e, method, request?.Resource.TargetType);
                }
                else if (e is AbortedUpdaterException)
                {
                    errorCode = ErrorCode.AbortedOperation;
                    errorResponse = AbortedOperation(e, method, request?.Resource.TargetType);
                }
                else if (e is AbortedDeleterException)
                {
                    errorCode = ErrorCode.AbortedOperation;
                    errorResponse = AbortedOperation(e, method, request?.Resource.TargetType);
                }
                else
                {
                    errorCode = UnknownError;
                    errorResponse = InternalError(e);
                }
                Error error = null;
                Error.ClearOld();
                Db.TransactAsync(() => error = new Error(errorCode, e, request));
                errorResponse.Headers["ErrorInfo"] = $"{_Uri}/{typeof(Error).FullName}/id={error.Id}";
                return errorResponse;
            }

            #endregion
        }
    }
}