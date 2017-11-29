using System;
using RESTar.Internal;
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
    /// <summary>
    /// Evaluates requests
    /// </summary>
    internal static class Evaluator
    {
        private static int StackSize;

        internal static Response Evaluate(HandlerActions action, Args args = null)
        {
            if (StackSize++ > 300) throw new InfiniteLoopException();
            IResource resource = null;
            try
            {
                resource = args?.IResource;
                switch (action)
                {
                    case GET:
                    case POST:
                    case PUT:
                    case PATCH:
                    case DELETE:
                    case COUNT: return HandleREST((dynamic) resource, args, action);
                    case ORIGIN: return HandleOrigin((dynamic) resource, args);
                    case VIEW: return HandleView((dynamic) resource, args);
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
                var error = Trans(() => Error.Create(errorInfo.Code, ex, resource, args, action));
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
                    case ORIGIN: return Forbidden("Invalid or unauthorized origin");
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
            finally
            {
                StackSize--;
            }
        }

        private static Response HandleView<T>(IResource<T> resource, Args args) where T : class
        {
            var request = new ViewRequest<T>(resource);
            request.Authenticate();
            request.Populate(args);
            request.MethodCheck();
            request.Evaluate();
            return request.GetView();
        }

        private static Response HandleREST<T>(IResource<T> resource, Args args,
            HandlerActions action) where T : class
        {
            using (var request = new RESTRequest<T>(resource))
            {
                request.Authenticate(ref args);
                request.Populate(args, (Methods) action);
                request.MethodCheck();
                request.SetRequestData(args.BodyBytes);
                request.RunResourceAuthentication();
                request.Evaluate();
                return request.GetResponse();
            }
        }

        private static Response HandleOrigin<T>(IResource<T> resource, Args args) where T : class
        {
            var origin = args.Headers.SafeGet("Origin");
            if (origin != null && (AllowAllOrigins || AllowedOrigins.Contains(new Uri(origin))))
                return AllowOrigin(origin, resource.AvailableMethods);
            return Forbidden("Invalid or unauthorized origin");
        }
    }
}