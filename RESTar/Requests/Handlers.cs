using System;
using RESTar.Internal;
using RESTar.View;
using Starcounter;
using static RESTar.Internal.Authenticator;
using static RESTar.Operations.Transact;
using static RESTar.Requests.HandlerActions;
using static Starcounter.SessionOptions;
using static RESTar.Settings;
using static RESTar.Requests.Responses;
using static RESTar.RESTarConfig;
using static Starcounter.Session;
using Error = RESTar.Admin.Error;
using IResource = RESTar.Internal.IResource;

namespace RESTar.Requests
{
    internal static class Handlers
    {
        internal static void Register(bool setupMenu)
        {
            var uri = _Uri + "{?}";
            Handle.GET(_Port, uri, (Request request, string query) => Evaluate(GET, request, query));
            Handle.POST(_Port, uri, (Request request, string query) => Evaluate(POST, request, query));
            Handle.PUT(_Port, uri, (Request request, string query) => Evaluate(PUT, request, query));
            Handle.PATCH(_Port, uri, (Request request, string query) => Evaluate(PATCH, request, query));
            Handle.DELETE(_Port, uri, (Request request, string query) => Evaluate(DELETE, request, query));
            Handle.OPTIONS(_Port, uri, (Request request, string query) => Evaluate(ORIGIN, request, query));
            if (!_ViewEnabled) return;
            Application.Current.Use(new HtmlFromJsonProvider());
            Application.Current.Use(new PartialToStandaloneHtmlProvider());
            var appName = Application.Current.Name;
            Handle.GET($"/{appName}{{?}}", (Request request, string query) => Evaluate(VIEW, request, query));
            Handle.GET("/__restar/__page", () => Evaluate(PAGE));
            if (!setupMenu) return;
            Handle.GET($"/{appName}", () => Evaluate(MENU));
        }

        private static Response Evaluate(HandlerActions action, Request request = null, string query = null)
        {
            IResource resource = null;
            try
            {
                var args = query?.ToArgs(request);
                resource = args?.IResource;
                switch (action)
                {
                    case GET:
                    case POST:
                    case PUT:
                    case PATCH:
                    case DELETE: return HandleREST((dynamic) resource, request, args, (RESTarMethods) action);
                    case ORIGIN: return HandleOrigin((dynamic) resource, request);
                    case VIEW: return HandleView((dynamic) resource, request, args);
                    case PAGE:
                        if (Current?.Data is View.Page) return Current.Data;
                        Current = Current ?? new Session(PatchVersioning);
                        return new View.Page {Session = Current};
                    case MENU:
                        CheckUser();
                        return new Menu().Populate().MakeCurrentView();
                    default: return UnknownHandlerAction;
                }
            }
            catch (Exception ex)
            {
                var errorInfo = ex.GetError();
                Error.ClearOld();
                var error = Trans(() => Error.Create(errorInfo.Code, ex, resource, request, action));
                switch (action)
                {
                    case GET:
                    case POST:
                    case PATCH:
                    case PUT:
                    case DELETE:
                        errorInfo.Response.Headers["ErrorInfo"] = $"{_Uri}/{typeof(Error).FullName}/id={error.Id}";
                        return errorInfo.Response;
                    case ORIGIN: return Forbidden;
                    case VIEW:
                    case PAGE:
                    case MENU:
                        var master = Self.GET<View.Page>("/__restar/__page");
                        var partial = master.CurrentPage as RESTarView ?? new MessageWindow().Populate();
                        partial.SetMessage(ex.Message, errorInfo.Code, MessageTypes.error);
                        master.CurrentPage = partial;
                        return master;
                    default: return InternalError(ex);
                }
            }
        }

        private static Response HandleView<T>(IResource<T> resource, Request scRequest, Args? args) where T : class
        {
            var request = new ViewRequest<T>(resource, scRequest);
            request.Authenticate();
            request.Populate(args.GetValueOrDefault());
            request.MethodCheck();
            request.Evaluate();
            return request.GetView();
        }

        private static Response HandleREST<T>(IResource<T> resource, Request scRequest, Args? args,
            RESTarMethods method) where T : class
        {
            using (var request = new RESTRequest<T>(resource, scRequest))
            {
                request.Authenticate();
                request.Populate(args.GetValueOrDefault(), method);
                request.MethodCheck();
                request.SetRequestData();
                request.Evaluate();
                return request.GetResponse();
            }
        }

        private static Response HandleOrigin<T>(IResource<T> resource, Request scRequest) where T : class
        {
            var origin = scRequest.Headers["Origin"];
            if (AllowAllOrigins || AllowedOrigins.Contains(new Uri(origin)))
                return AllowOrigin(origin, resource.AvailableMethods);
            return Forbidden;
        }
    }
}