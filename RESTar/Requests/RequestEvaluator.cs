using System;
using RESTar.Admin;
using RESTar.Internal;
using RESTar.Operations;
using RESTar.Results.Error;
using RESTar.Results.Fail.BadRequest.Aborted;
using RESTar.Results.Fail.Forbidden;
using RESTar.Results.Success;
using static RESTar.Operations.Transact;
using static RESTar.Requests.Action;
using static RESTar.RESTarConfig;
using Console = RESTar.Admin.Console;
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
            ref string query,
            byte[] body,
            Headers headers,
            TCPConnection tcpConnection
        )
        {
            if (StackDepth++ > 300) throw new InfiniteLoop();
            var (arguments, result) = (default(Arguments), default(IFinalizedResult));

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
                        arguments = new Arguments(action, ref query, body, headers, tcpConnection);
                        arguments.Authenticate();
                        arguments.ThrowIfError();
                        switch (arguments.IResource)
                        {
                            case TerminalResource terminalResource:
                                if (!tcpConnection.HasWebSocket)
                                    return result = new UpgradeRequired(terminalResource.Name);
                                terminalResource.InstantiateFor(tcpConnection.WebSocketInternal, arguments.Uri.Conditions);
                                return result = new WebSocketResult(true, arguments);
                            case var entityResource:
                                result = HandleREST((dynamic) entityResource, arguments);
                                if (!tcpConnection.HasWebSocket || tcpConnection.Origin == OriginType.Shell)
                                    return result;
                                tcpConnection.WebSocketInternal.SendResult(result);
                                return result = new WebSocketResult(false, arguments);
                        }
                    case OPTIONS:
                        arguments = new Arguments(action, ref query, body, headers, tcpConnection);
                        arguments.ThrowIfError();
                        return result = HandleOptions(arguments.IResource, arguments);

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
            catch (Exception exs)
            {
                var error = RESTarError.GetError(exs);
                error.SetTrace(tcpConnection);
                string errorId = null;
                if (!(error is Forbidden))
                {
                    Error.ClearOld();
                    errorId = Trans(() => Error.Create(error, arguments)).Id;
                }
                if (tcpConnection.HasWebSocket && tcpConnection.Origin != OriginType.Shell)
                {
                    tcpConnection.WebSocketInternal.SendResult(error);
                    return result = new WebSocketResult(false, error);
                }
                if (tcpConnection.Origin == OriginType.Shell)
                {
                    var shell = (Shell) tcpConnection.WebSocketInternal.Terminal;
                    shell.GoToPrevious();
                }

                switch (action)
                {
                    case GET:
                    case POST:
                    case PATCH:
                    case PUT:
                    case DELETE:
                    case REPORT:
                        if (errorId != null)
                            error.Headers["ErrorInfo"] = $"/{typeof(Error).FullName}/id={errorId}";
                        return result = error;
                    case OPTIONS: return result = new InvalidOrigin();
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
                if (!(result is WebSocketResult) && tcpConnection.Origin != OriginType.Shell)
                    Console.Log(arguments, result);
                StackDepth--;
            }
        }

        private static Response HandleView<T>(IEntityResource<T> resource, Arguments arguments) where T : class
        {
            var request = new ViewRequest<T>(resource, arguments.TcpConnection);
            request.Authenticate();
            request.Populate(arguments);
            request.MethodCheck();
            request.Evaluate();
            return null; //request.GetView();
        }

        private static IFinalizedResult HandleREST<T>(IEntityResource<T> resource, Arguments arguments)
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
                    throw new AbortedSelect<T>(e, request);
                }
            }
        }

        private static IFinalizedResult HandleOptions(IResource resource, Arguments arguments)
        {
            var origin = arguments.Headers.SafeGet("Origin");
            if (origin != null && (AllowAllOrigins || AllowedOrigins.Contains(new Uri(origin))))
                return new AcceptOrigin(origin, resource, arguments);
            return new InvalidOrigin();
        }
    }
}