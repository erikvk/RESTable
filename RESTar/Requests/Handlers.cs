using System;
using RESTar.Internal;
using RESTar.Operations;
using RESTar.View;
using Starcounter;
using static RESTar.Internal.Authenticator;
using static RESTar.Operations.Transact;
using static RESTar.Requests.HandlerActions;
using static Starcounter.SessionOptions;
using static RESTar.Admin.Settings;
using static RESTar.Requests.Responses;
using static RESTar.RESTarConfig;
using static Starcounter.Session;
using Error = RESTar.Admin.Error;
using IResource = RESTar.Internal.IResource;

namespace RESTar.Requests
{
    internal static class Handlers
    {
        internal static void RegisterRESTHandlers(bool setupMenu)
        {
            var uri = _Uri + "{?}";
            Handle.GET(_Port, uri, (Request request, string query) => Evaluate(GET, request, query));
            Handle.POST(_Port, uri, (Request request, string query) => Evaluate(POST, request, query));
            Handle.PUT(_Port, uri, (Request request, string query) => Evaluate(PUT, request, query));
            Handle.PATCH(_Port, uri, (Request request, string query) => Evaluate(PATCH, request, query));
            Handle.DELETE(_Port, uri, (Request request, string query) => Evaluate(DELETE, request, query));
            Handle.CUSTOM(_Port, "REPORT " + uri, (Request request, string query) => Evaluate(COUNT, request, query));
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
                    case DELETE:
                    case COUNT: return HandleREST((dynamic) resource, request, args, action);
                    case ORIGIN: return HandleOrigin((dynamic) resource, request);
                    case VIEW: return HandleView((dynamic) resource, request, args);
                    case PAGE:
#pragma warning disable 618
                        if (Current?.Data is View.Page) return Current.Data;
                        Current = Current ?? new Session(PatchVersioning);
                        return new View.Page {Session = Current};
#pragma warning restore 618
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
                    case COUNT:
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
            HandlerActions action) where T : class
        {
            var arguments = args.GetValueOrDefault();
            using (var request = new RESTRequest<T>(resource, scRequest))
            {
                request.Authenticate(ref arguments);
                request.Populate(arguments, (Methods) action);
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

        internal static void UnRegisterRESTHandlers()
        {
            var uri = _Uri + "{?}";
            Do.Try(() => Handle.UnregisterHttpHandler(_Port, "GET", uri));
            Do.Try(() => Handle.UnregisterHttpHandler(_Port, "POST", uri));
            Do.Try(() => Handle.UnregisterHttpHandler(_Port, "PUT", uri));
            Do.Try(() => Handle.UnregisterHttpHandler(_Port, "PATCH", uri));
            Do.Try(() => Handle.UnregisterHttpHandler(_Port, "DELETE", uri));
            Do.Try(() => Handle.UnregisterHttpHandler(_Port, "OPTIONS", uri));
            var appName = Application.Current.Name;
            Do.Try(() => Handle.UnregisterHttpHandler(_Port, "GET", $"/{appName}{{?}}"));
            Do.Try(() => Handle.UnregisterHttpHandler(_Port, "GET", "/__restar/__page"));
            Do.Try(() => Handle.UnregisterHttpHandler(_Port, "GET", $"/{appName}"));
        }
    }
}