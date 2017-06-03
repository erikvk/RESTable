using System;
using Newtonsoft.Json;
using RESTar.Internal;
using RESTar.Operations;
using RESTar.View;
using Starcounter;
using static RESTar.Internal.ErrorCodes;
using static RESTar.RESTarMethods;
using static Starcounter.SessionOptions;
using static RESTar.Settings;
using ScRequest = Starcounter.Request;
using ScHandle = Starcounter.Handle;

namespace RESTar.Requests
{
    internal static class Handlers
    {
        internal static void Register(bool setupMenu)
        {
            var uri = _Uri + "{?}";
            ScHandle.GET(_Port, uri, (ScRequest r, string q) => Handle(r, q, Evaluators.GET, GET));
            ScHandle.POST(_Port, uri, (ScRequest r, string q) => Handle(r, q, Evaluators.POST, POST));
            ScHandle.PUT(_Port, uri, (ScRequest r, string q) => Handle(r, q, Evaluators.PUT, PUT));
            ScHandle.PATCH(_Port, uri, (ScRequest r, string q) => Handle(r, q, Evaluators.PATCH, PATCH));
            ScHandle.DELETE(_Port, uri, (ScRequest r, string q) => Handle(r, q, Evaluators.DELETE, DELETE));
            ScHandle.OPTIONS(_Port, uri, (ScRequest r, string q) => CheckOrigin(r, q));

            if (!_ViewEnabled) return;

            Application.Current.Use(new HtmlFromJsonProvider());
            Application.Current.Use(new PartialToStandaloneHtmlProvider());

            ScHandle.GET("/__restar/__page", () =>
            {
                if (Session.Current?.Data is View.Page)
                    return Session.Current.Data;
                Session.Current = Session.Current ?? new Session(PatchVersioning);
                return new View.Page {Session = Session.Current};
            });

            ScHandle.GET($"/{Application.Current.Name}{{?}}", (ScRequest r, string query) =>
            {
                try
                {
                    Authenticator.UserCheck();
                    using (var request = new Request(r))
                    {
                        request.Populate(query, GET, Evaluators.VIEW);
                        request.MetaConditions.DeactivateProcessors();
                        if (!request.Resource.Viewable)
                            throw new ForbiddenException(NotAuthorized, "Resource is not viewable");
                        request.MethodCheck();
                        request.Evaluate();
                        var partial = (Json) request.GetResponse();
                        var master = Self.GET<View.Page>("/__restar/__page");
                        master.CurrentPage = partial ?? master.CurrentPage;
                        return master;
                    }
                }
                catch (Exception e)
                {
                    return RESTarException.HandleViewException(e);
                }
            });

            if (!setupMenu) return;
            ScHandle.GET($"/{Application.Current.Name}", () =>
            {
                try
                {
                    Authenticator.UserCheck();
                    var partial = new Menu().Populate();
                    var master = Self.GET<View.Page>("/__restar/__page");
                    master.CurrentPage = partial;
                    return master;
                }
                catch (Exception e)
                {
                    return RESTarException.HandleViewException(e);
                }
            });
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

        private static Response Handle(ScRequest scRequest, string query, Evaluator evaluator,
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
                    return request.GetResponse();
                }
            }

            #region Catch exceptions

            catch (Exception e)
            {
                Response errorResponse;
                ErrorCodes errorCode;

                if (e is NoContentException) return Responses.NoContent();

                if (e is RESTarException re)
                {
                    errorCode = re.ErrorCode;
                    errorResponse = re.Response;
                }
                else if (e is FormatException)
                {
                    errorCode = UnsupportedContentType;
                    errorResponse = Responses.BadRequest(e);
                }
                else if (e is JsonReaderException)
                {
                    errorCode = JsonDeserializationError;
                    errorResponse = Responses.DeserializationError(scRequest.Body);
                }
                else if (e is DbException)
                {
                    errorCode = DatabaseError;
                    errorResponse = Responses.DatabaseError(e);
                }
                else
                {
                    errorCode = UnknownError;
                    errorResponse = Responses.InternalError(e);
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