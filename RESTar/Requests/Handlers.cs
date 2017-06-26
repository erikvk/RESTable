using System;
using Newtonsoft.Json;
using RESTar.Internal;
using RESTar.Operations;
using RESTar.View;
using Starcounter;
using static RESTar.Internal.ErrorCodes;
using static RESTar.Requests.Responses;
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
                    using (var request = new HttpRequest(r))
                    {
                        request.Populate(query, GET, Evaluators.VIEW);
                        request.MetaConditions.DeactivateProcessors();
                        if (!request.Resource.IsViewable)
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
                var request = new HttpRequest(scRequest);
                request.Populate(query, default(RESTarMethods), null);
                var origin = request.ScRequest.Headers["Origin"].ToLower();
                if (RESTarConfig.AllowAllOrigins || RESTarConfig.AllowedOrigins.Contains(new Uri(origin)))
                    return AllowOrigin(origin, request.Resource.AvailableMethods);
                return Forbidden();
            }
            catch
            {
                return Forbidden();
            }
        }

        private static Response Handle(ScRequest scRequest, string query, Evaluator evaluator,
            RESTarMethods method)
        {
            HttpRequest request = null;
            try
            {
                using (request = new HttpRequest(scRequest))
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
                (ErrorCodes code, Response response) getError(Exception ex)
                {
                    switch (ex)
                    {
                        case RESTarException re: return (re.ErrorCode, re.Response);
                        case FormatException _: return (UnsupportedContentType, BadRequest(ex));
                        case JsonReaderException _: return (JsonDeserializationError, JsonError(scRequest.Body));
                        case DbException _: return (DatabaseError, DbError(ex));
                        default: return (UnknownError, InternalError(ex));
                    }
                }

                var errorInfo = getError(e);
                Error error = null;
                Error.ClearOld();
                Db.TransactAsync(() => error = new Error(errorInfo.code, e, request));
                errorInfo.response.Headers["ErrorInfo"] = $"{_Uri}/{typeof(Error).FullName}/id={error.Id}";
                return errorInfo.response;
            }

            #endregion
        }
    }
}