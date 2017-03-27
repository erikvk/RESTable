using System;
using System.Collections.Generic;
using Jil;
using Newtonsoft.Json;
using Starcounter;
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
                var request = new Request(scRequest, query, default(RESTarMethods), null);
                var origin = request.ScRequest.Headers["Origin"].ToLower();
                Log.Info("Options request: Origin " + origin);
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
                var access = Authenticate(scRequest);
                request = new Request(scRequest, query, method, evaluator);
                request.ResolveMethod();
                MethodCheck(request, access);
                request.ResolveDataSource();
                var response = request.Evaluate();
                return request.GetResponse(response);
            }       
            #region Catch exceptions

            catch (DeserializationException e)
            {
                if (e.InnerException != null)
                    return BadRequest(e.InnerException);
                return DeserializationError(scRequest.Body);
            }
            catch (JsonSerializationException e)
            {
                if (e.InnerException != null)
                    return BadRequest(e.InnerException);
                return DeserializationError(scRequest.Body);
            }
            catch (SqlException e)
            {
                return SemanticsError(e);
            }
            catch (SyntaxException e)
            {
                return BadRequest(e);
            }
            catch (UnknownColumnException e)
            {
                return NotFound(e);
            }
            catch (CustomEntityUnknownColumnException e)
            {
                return NotFound(e);
            }
            catch (AmbiguousColumnException e)
            {
                return AmbiguousColumn(e);
            }
            catch (ExternalSourceException e)
            {
                return BadRequest(e);
            }
            catch (UnknownResourceException e)
            {
                return NotFound(e);
            }
            catch (UnknownResourceForMappingException e)
            {
                return NotFound(e);
            }
            catch (AmbiguousResourceException e)
            {
                return AmbiguousResource(e);
            }
            catch (InvalidInputCountException e)
            {
                return BadRequest(e);
            }
            catch (AmbiguousMatchException e)
            {
                return AmbiguousMatch(e.Resource.TargetType);
            }
            catch (ExcelInputException e)
            {
                return BadRequest(e);
            }
            catch (ExcelFormatException e)
            {
                return BadRequest(e);
            }
            catch (RESTarInternalException e)
            {
                return RESTarInternalError(e);
            }
            catch (NoContentException)
            {
                return NoContent();
            }
            catch (JsonReaderException)
            {
                return DeserializationError(scRequest.Body);
            }
            catch (DbException e)
            {
                return DatabaseError(e);
            }
            catch (AbortedSelectorException e)
            {
                return AbortedOperation(e, method, request?.Resource.TargetType);
            }
            catch (AbortedInserterException e)
            {
                return AbortedOperation(e, method, request?.Resource.TargetType);
            }
            catch (AbortedUpdaterException e)
            {
                return AbortedOperation(e, method, request?.Resource.TargetType);
            }
            catch (AbortedDeleterException e)
            {
                return AbortedOperation(e, method, request?.Resource.TargetType);
            }
            catch (ForbiddenException e)
            {
                return Forbidden();
            }
            catch (Exception e)
            {
                return InternalError(e);
            }

            #endregion
        }

        private static void MethodCheck(IRequest request, AccessRights accessRights)
        {
            var method = request.Method;
            var availableMethods = request.Resource.AvailableMethods;
            if (!availableMethods.Contains(method))
                throw new ForbiddenException();
            if (!RequireApiKey)
                return;
            if (accessRights == null)
                throw new ForbiddenException();
            var rights = accessRights[request.Resource];
            if (rights == null || !rights.Contains(method))
                throw new ForbiddenException();
        }

        private static AccessRights Authenticate(ScRequest request)
        {
            if (!RequireApiKey)
                return null;
            var authorizationHeader = request.Headers["Authorization"];
            if (string.IsNullOrWhiteSpace(authorizationHeader))
                throw new ForbiddenException();
            var apikey_key = authorizationHeader.Split(' ');
            if (apikey_key[0].ToLower() != "apikey" || apikey_key.Length != 2)
                throw new ForbiddenException();
            AccessRights accessRights;
            if (!ApiKeys.TryGetValue(apikey_key[1].MD5(), out accessRights))
                throw new ForbiddenException();
            return accessRights;
        }
    }
}