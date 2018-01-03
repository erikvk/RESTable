using System;
using System.Collections.Generic;
using RESTar.Internal;
using RESTar.Operations;
using RESTar.Results.Error;
using RESTar.Results.Success;
using static RESTar.Operations.Transact;
using static RESTar.Requests.Action;
using static RESTar.Internal.ErrorCodes;
using static RESTar.RESTarConfig;
using Error = RESTar.Admin.Error;
using IResource = RESTar.Internal.IResource;
using Response = RESTar.Http.HttpResponse;

namespace RESTar.Requests
{
    /// <summary>
    /// Evaluates requests
    /// </summary>
    internal static class RequestEvaluator
    {
        private static int StackDepth;

        internal static IFinalizedResult Evaluate
        (
            Action action,
            string query,
            byte[] body,
            Dictionary<string, string> headers,
            Origin origin
        )
        {
            if (StackDepth++ > 300) throw new InfiniteLoop();
            Arguments arguments = null;
            try
            {
                switch (action)
                {
                    case GET:
                    case POST:
                    case PUT:
                    case PATCH:
                    case DELETE:
                    case REPORT:
                        arguments = new Arguments(action, query, body, headers, origin);
                        arguments.Authenticate();
                        arguments.ThrowIfError();
                        return HandleREST((dynamic) arguments.IResource, arguments);

                    case OPTIONS:
                        arguments = new Arguments(action, query, body, headers, origin);
                        arguments.ThrowIfError();
                        return HandleOptions(arguments.IResource, arguments);

                    // case VIEW: return HandleView((dynamic) resource, arguments);
                    // case PAGE:
                    // #pragma warning disable 618
                    //     if (Current?.Data is View.Page) return Current.Data;
                    //     Current = Current ?? new Session(PatchVersioning);
                    //     return new View.Page {Session = Current};
                    // #pragma warning restore 618
                    // case MENU:
                    //     CheckUser();
                    //     return new Menu().Populate().MakeCurrentView();

                    default: throw new Exception();
                }
            }
            catch (Exception ex)
            {
                var (code, result) = ex.GetError();
                Error.ClearOld();
                var error = Trans(() => Error.Create(code, ex, arguments, action));
                switch (action)
                {
                    case GET:
                    case POST:
                    case PATCH:
                    case PUT:
                    case DELETE:
                    case REPORT:
                        result.Headers["ErrorInfo"] = $"/{typeof(Error).FullName}/id={error.Id}";
                        return result;
                    case OPTIONS: return new Forbidden(NotAuthorized, "Invalid or unauthorized origin");
                    //case VIEW:
                    //case PAGE:
                    //case MENU:
                    //    var master = Self.GET<View.Page>("/__restar/__page");
                    //    var partial = master.CurrentPage as RESTarView ?? new MessageWindow().Populate();
                    //    partial.SetMessage(ex.Message, code, MessageTypes.error);
                    //    master.CurrentPage = partial;
                    //    return master;
                    default: throw new Exception();
                }
            }
            finally
            {
                StackDepth--;
            }
        }

        private static Response HandleView<T>(IResource<T> resource, Arguments arguments) where T : class
        {
            var request = new ViewRequest<T>(resource, arguments.Origin);
            request.Authenticate();
            request.Populate(arguments);
            request.MethodCheck();
            request.Evaluate();
            return null; //request.GetView();
        }

        private static IFinalizedResult HandleREST<T>(IResource<T> resource, Arguments arguments)
            where T : class
        {
            using (var request = new RESTRequest<T>(resource, arguments))
            {
                request.RunResourceAuthentication();
                request.Evaluate();
                try
                {
                    return arguments.ResultFinalizer.Invoke(request.Result);
                }
                catch (Exception e)
                {
                    throw new AbortedSelectorException<T>(e, request);
                }
            }
        }

        private static IFinalizedResult HandleOptions(IResource resource, Arguments arguments)
        {
            var origin = arguments.Headers.SafeGet("Origin");
            if (origin != null && (AllowAllOrigins || AllowedOrigins.Contains(new Uri(origin))))
                return new AcceptOrigin(origin, resource);
            return new Forbidden(NotAuthorized, "Invalid or unauthorized origin");
        }
    }
}