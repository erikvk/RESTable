﻿using System;
using System.Net;
using RESTar.Internal;
using RESTar.Operations;
using static RESTar.Operations.Transact;
using static RESTar.Requests.HandlerActions;
using static RESTar.Internal.ErrorCodes;
using static RESTar.RESTarConfig;
using Error = RESTar.Admin.Error;
using IResource = RESTar.Internal.IResource;
using Response = RESTar.Http.HttpResponse;

namespace RESTar.Requests
{
    internal delegate IFinalizedResult Evaluator(HandlerActions action, Func<Arguments> argsMaker);

    /// <summary>
    /// Evaluates requests
    /// </summary>
    internal static class RequestEvaluator
    {
        private static int StackDepth;

        internal static readonly Evaluator Evaluate = (action, argsMaker) =>
        {
            if (StackDepth++ > 300) throw new InfiniteLoop();
            IResource resource = null;
            Arguments arguments = null;
            try
            {
                arguments = argsMaker?.Invoke();
                resource = arguments?.IResource;
                switch (action)
                {
                    case GET:
                    case POST:
                    case PUT:
                    case PATCH:
                    case DELETE:
                    case REPORT: return HandleREST((dynamic) resource, arguments, action);
                    case OPTIONS: return HandleOptions(resource, arguments);
                    case VIEW: return HandleView((dynamic) resource, arguments);
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
                var error = Trans(() => Error.Create(code, ex, resource, arguments, action));
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
        };

        private static Response HandleView<T>(IResource<T> resource, Arguments arguments) where T : class
        {
            var request = new ViewRequest<T>(resource, arguments.Origin);
            request.Authenticate();
            request.Populate(arguments);
            request.MethodCheck();
            request.Evaluate();
            return null; //request.GetView();
        }

        private static IFinalizedResult HandleREST<T>(IResource<T> resource, Arguments arguments, HandlerActions action)
            where T : class
        {
            using (var request = new RESTRequest<T>(resource, arguments.Origin))
            {
                request.Authenticate(arguments);
                request.Populate(arguments, (Methods) action);
                request.MethodCheck();
                request.SetRequestData(arguments.BodyBytes);
                request.RunResourceAuthentication();
                request.Evaluate();
                return request.GetFinalizedResult();
            }
        }

        private static IFinalizedResult HandleOptions(IResource resource, Arguments arguments)
        {
            var origin = arguments.Headers.SafeGetNoCase("origin");
            if (origin != null && (AllowAllOrigins || AllowedOrigins.Contains(new Uri(origin))))
                return new Result
                {
                    StatusCode = HttpStatusCode.OK,
                    StatusDescription = "OK",
                    Headers =
                    {
                        ["Access-Control-Allow-Origin"] = AllowAllOrigins ? "*" : origin,
                        ["Access-Control-Allow-Methods"] = string.Join(", ", resource.AvailableMethods),
                        ["Access-Control-Max-Age"] = "120",
                        ["Access-Control-Allow-Credentials"] = "true",
                        ["Access-Control-Allow-Headers"] = "origin, content-type, accept, authorization, source, destination"
                    }
                };
            return new Forbidden(NotAuthorized, "Invalid or unauthorized origin");
        }
    }
}