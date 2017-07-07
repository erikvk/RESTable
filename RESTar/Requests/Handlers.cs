using System;
using Newtonsoft.Json;
using RESTar.Internal;
using RESTar.View;
using Starcounter;
using static RESTar.Internal.ErrorCodes;
using static RESTar.Requests.HandlerActions;
using static Starcounter.SessionOptions;
using static RESTar.Settings;
using static RESTar.Requests.Responses;
using static RESTar.RESTarConfig;

namespace RESTar.Requests
{
    internal static class Handlers
    {
        internal static void Register(bool setupMenu)
        {
            var restUri = _Uri + "{?}";
            Handle.GET(_Port, restUri, (Request request, string query) => Handler(request, query, GET));
            Handle.POST(_Port, restUri, (Request request, string query) => Handler(request, query, POST));
            Handle.PUT(_Port, restUri, (Request request, string query) => Handler(request, query, PUT));
            Handle.PATCH(_Port, restUri, (Request request, string query) => Handler(request, query, PATCH));
            Handle.DELETE(_Port, restUri, (Request request, string query) => Handler(request, query, DELETE));
            Handle.OPTIONS(_Port, restUri, (Request request, string query) => Handler(request, query, ORIGIN));
            if (!_ViewEnabled) return;
            Application.Current.Use(new HtmlFromJsonProvider());
            Application.Current.Use(new PartialToStandaloneHtmlProvider());
            var appName = Application.Current.Name;
            Handle.GET($"/{appName}{{?}}", (Request request, string query) => Handler(request, query, VIEW));
            Handle.GET("/__restar/__page", () => Handler(PAGE));
            if (!setupMenu) return;
            Handle.GET($"/{appName}", () => Handler(MENU));
        }

        private static Response Handler(HandlerActions action)
        {
            switch (action)
            {
                case PAGE: return HandlePage();
                case MENU: return HandleMenu();
                default: return UnknownAction;
            }
        }

        private static Response Handler(Request request, string query, HandlerActions action)
        {
            var args = query.ToArgs(request);
            var resource = args.Length == 1 ? Resource.MetaResource : args[1].FindResource();
            try
            {
                switch (action)
                {
                    case GET:
                    case POST:
                    case PUT:
                    case PATCH:
                    case DELETE: return HandleREST((dynamic) resource, request, args, (RESTarMethods) action);
                    case ORIGIN: return HandleOrigin((dynamic) resource, request);
                    case VIEW: return HandleView((dynamic) resource, request, args);
                    default: return UnknownAction;
                }
            }
            catch (Exception e)
            {
                (ErrorCodes code, Response response) getError(Exception ex)
                {
                    switch (ex)
                    {
                        case RESTarException re: return (re.ErrorCode, re.Response);
                        case FormatException _: return (UnsupportedContentType, BadRequest(ex));
                        case JsonReaderException _: return (JsonDeserializationError, JsonError);
                        case DbException _: return (DatabaseError, DbError(ex));
                        default: return (UnknownError, InternalError(ex));
                    }
                }

                var errorInfo = getError(e);
                Error error = null;
                Error.ClearOld();
                Db.TransactAsync(() => error = Error.Create(errorInfo.code, e, resource, request, action));
                errorInfo.response.Headers["ErrorInfo"] = $"{_Uri}/{typeof(Error).FullName}/id={error.Id}";
                return errorInfo.response;
            }
        }

        private static Response HandleMenu()
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
        }

        private static Response HandlePage()
        {
            if (Session.Current?.Data is View.Page)
                return Session.Current.Data;
            Session.Current = Session.Current ?? new Session(PatchVersioning);
            return new View.Page {Session = Session.Current};
        }

        private static Response HandleOrigin<T>(IResource<T> resource, Request scRequest) where T : class
        {
            try
            {
                var origin = scRequest.Headers["Origin"];
                if (AllowAllOrigins || AllowedOrigins.Contains(new Uri(origin)))
                    return AllowOrigin(origin, resource.AvailableMethods);
                return Forbidden;
            }
            catch
            {
                return Forbidden;
            }
        }

        private static Response HandleView<T>(IResource<T> resource, Request scRequest, string[] args) where T : class
        {
            return null;
            //Authenticator.UserCheck();
            //using (var request = new ViewRequest<T>(resource))
            //{
            //    request.Populate(query, GET, RESTEvaluators.VIEW);
            //    request.MetaConditions.DeactivateProcessors();
            //    request.MethodCheck();
            //    request.Evaluate();
            //    var partial = (Json) request.GetResponse();
            //    var master = Self.GET<View.Page>("/__restar/__page");
            //    master.CurrentPage = partial ?? master.CurrentPage;
            //    return master;
            //}
        }

        private static Response HandleREST<T>(IResource<T> resource, Request scRequest, string[] args,
            RESTarMethods method) where T : class
        {
            using (var request = new RESTRequest<T>(resource, scRequest))
            {
                request.Authenticate();
                request.Populate(args, method);
                request.MethodCheck();
                request.GetRequestData();
                request.Evaluate();
                return request.GetResponse();
            }
        }
    }
}