using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using RESTar.Admin;
using RESTar.ContentTypeProviders;
using RESTar.Internal;
using RESTar.Linq;
using RESTar.Operations;
using RESTar.Results.Error;
using RESTar.Results.Error.Forbidden;
using RESTar.Results.Success;
using RESTar.Serialization;
using static RESTar.Operations.Transact;
using static RESTar.Methods;
using static RESTar.RESTarConfig;
using Error = RESTar.Admin.Error;
using Response = RESTar.Http.HttpResponse;

namespace RESTar.Requests
{
    internal class CachedProtocolProvider
    {
        internal IProtocolProvider ProtocolProvider { get; }
        internal IDictionary<string, IContentTypeProvider> InputContentTypeProviders { get; }
        internal IDictionary<string, IContentTypeProvider> OutputContentTypeProviders { get; }
        internal ContentType DefaultInputContentType { get; set; }
        internal ContentType DefaultOutputContentType { get; set; }
        internal IContentTypeProvider DefaultInputProvider { get; set; }
        internal IContentTypeProvider DefaultOutputProvider { get; set; }

        public CachedProtocolProvider(IProtocolProvider protocolProvider)
        {
            ProtocolProvider = protocolProvider;
            InputContentTypeProviders = new Dictionary<string, IContentTypeProvider>(StringComparer.OrdinalIgnoreCase);
            OutputContentTypeProviders = new Dictionary<string, IContentTypeProvider>(StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Evaluates requests
    /// </summary>
    public static class RequestEvaluator
    {
        #region Handle addons

        internal static IDictionary<string, CachedProtocolProvider> ProtocolProviders { get; private set; }
        private static IDictionary<string, IContentTypeProvider> InputContentTypeProviders { get; set; }
        private static IDictionary<string, IContentTypeProvider> OutputContentTypeProviders { get; set; }

        private static CachedProtocolProvider GetCachedProtocolProvider(IProtocolProvider provider)
        {
            var cachedProvider = new CachedProtocolProvider(provider);
            var contentTypeProviders = provider.GetContentTypeProviders()?.ToList();
            contentTypeProviders?.ForEach(contentTypeProvider =>
            {
                contentTypeProvider.CanRead()?.ForEach(contentType => cachedProvider
                    .InputContentTypeProviders[contentType.MimeType] = contentTypeProvider);
                contentTypeProvider.CanWrite()?.ForEach(contentType => cachedProvider
                    .OutputContentTypeProviders[contentType.MimeType] = contentTypeProvider);
            });
            if (provider.AllowExternalContentProviders)
            {
                InputContentTypeProviders.ForEach(externalProvider =>
                {
                    if (!cachedProvider.InputContentTypeProviders.ContainsKey(externalProvider.Key))
                        cachedProvider.InputContentTypeProviders.Add(externalProvider);
                });
                OutputContentTypeProviders.ForEach(externalProvider =>
                {
                    if (!cachedProvider.OutputContentTypeProviders.ContainsKey(externalProvider.Key))
                        cachedProvider.OutputContentTypeProviders.Add(externalProvider);
                });
            }

            cachedProvider.DefaultInputProvider = cachedProvider.InputContentTypeProviders.Values.FirstOrDefault(p =>
            {
                if (p.CanRead()?.Any() != true) return false;
                cachedProvider.DefaultInputContentType = p.CanRead().First();
                return true;
            });
            cachedProvider.DefaultOutputProvider = cachedProvider.OutputContentTypeProviders.Values.FirstOrDefault(p =>
            {
                if (p.CanWrite()?.Any() != true) return false;
                cachedProvider.DefaultOutputContentType = p.CanWrite().First();
                return true;
            });

            return cachedProvider;
        }

        private static void ValidateProtocolProvider(IProtocolProvider provider)
        {
            if (provider == null)
                throw new InvalidProtocolProvider("External protocol provider cannot be null");
            if (string.IsNullOrWhiteSpace(provider.ProtocolIdentifier))
                throw new InvalidProtocolProvider($"Invalid protocol provider '{provider.GetType().RESTarTypeName()}'. " +
                                                  "ProtocolIdentifier cannot be null or whitespace");
            if (!Regex.IsMatch(provider.ProtocolIdentifier, "^[a-zA-Z]+$"))
                throw new InvalidProtocolProvider($"Invalid protocol provider '{provider.GetType().RESTarTypeName()}'. " +
                                                  "ProtocolIdentifier can only contain letters a-z and A-Z");
            if (!provider.AllowExternalContentProviders)
            {
                var contentProviders = provider.GetContentTypeProviders()?.ToList();
                if (contentProviders?.Any() != true)
                    throw new InvalidProtocolProvider($"Invalid protocol provider '{provider.GetType().RESTarTypeName()}'. " +
                                                      "The protocol provider allows no external content type providers " +
                                                      "and does not provide any content type providers of its own.");
                if (contentProviders.All(p => p.CanRead()?.Any() != true) && contentProviders.All(p => p.CanWrite()?.Any() != true))
                    throw new InvalidProtocolProvider($"Invalid protocol provider '{provider.GetType().RESTarTypeName()}'. " +
                                                      "The protocol provider allows no external content type providers " +
                                                      "and none of the provided content type providers can read or write.");
            }
        }

        private static void ValidateContentTypeProvider(IContentTypeProvider provider)
        {
            if (provider == null)
                throw new InvalidContentTypeProvider("External content type provider cannot be null");
            if (provider.CanRead()?.Any() != true && provider.CanWrite()?.Any() != true)
                throw new InvalidContentTypeProvider($"Provider '{provider.GetType().RESTarTypeName()}' cannot read or write to any formats");
        }

        internal static void SetupContentTypeProviders(List<IContentTypeProvider> contentTypeProviders)
        {
            InputContentTypeProviders = new Dictionary<string, IContentTypeProvider>(StringComparer.OrdinalIgnoreCase);
            OutputContentTypeProviders = new Dictionary<string, IContentTypeProvider>(StringComparer.OrdinalIgnoreCase);
            contentTypeProviders = contentTypeProviders ?? new List<IContentTypeProvider>();
            contentTypeProviders.Insert(0, new XMLWriter());
            contentTypeProviders.Insert(0, Serializers.Excel);
            contentTypeProviders.Insert(0, Serializers.Json);
            foreach (var provider in contentTypeProviders)
            {
                ValidateContentTypeProvider(provider);
                provider.CanRead()?.ForEach(contentType => InputContentTypeProviders[contentType.MimeType] = provider);
                provider.CanWrite()?.ForEach(contentType => OutputContentTypeProviders[contentType.MimeType] = provider);
            }
        }

        internal static void SetupProtocolProviders(List<IProtocolProvider> protocolProviders)
        {
            ProtocolProviders = new Dictionary<string, CachedProtocolProvider>(StringComparer.OrdinalIgnoreCase);
            protocolProviders = protocolProviders ?? new List<IProtocolProvider>();
            protocolProviders.Add(new DefaultProtocolProvider());
            protocolProviders.ForEach(provider =>
            {
                ValidateProtocolProvider(provider);
                var cachedProvider = GetCachedProtocolProvider(provider);
                if (provider is DefaultProtocolProvider)
                    ProtocolProviders[""] = cachedProvider;
                var protocolId = "-" + provider.ProtocolIdentifier;
                if (ProtocolProviders.TryGetValue(protocolId, out var existing))
                {
                    if (existing.GetType() == provider.GetType())
                        throw new InvalidProtocolProvider(
                            $"A protocol provider of type '{existing.GetType()}' has already been added");
                    throw new InvalidProtocolProvider(
                        $"Protocol identifier '{protocolId}' already claimed by a protocol provider of type '{existing.GetType()}'");
                }
                ProtocolProviders[protocolId] = cachedProvider;
            });
        }

        #endregion

        private static int StackDepth;

        /// <summary>
        /// Evaluates a request through the RESTar request engine. The return is a 2-tuple containing function delegates.
        /// Invoking the first one gets a raw view of the request result. It is preferable when we want to work with the 
        /// generated result as .NET types, and when there is no intention to create an output stream from the result.
        /// Invoking the second one gets a finalized view of the request result, where entities are serialized to output 
        /// streams (where applicable). Use this if an output stream is needed for the result content.
        /// </summary>
        /// <param name="trace">Include a trace, for example a TCPConnection (cannot be null)</param>
        /// <param name="method">The method to perform</param>
        /// <param name="uri">The URI of the request (cannot be null)</param>
        /// <param name="body">The body of the request (can be null)</param>
        /// <param name="headers">The headers of the request (can be null)</param>
        /// <returns></returns>
        public static (Func<IResult> GetRawResult, Func<IFinalizedResult> GetFinalizedResult) Evaluate(ITraceable trace, Methods method, ref string uri,
            byte[] body = null, Headers headers = null)
        {
            if (uri == null) throw new MissingUri();

            var (context, result) = RunEvaluation
            (
                method: method,
                uri: ref uri,
                body: body,
                headers: headers ?? new Headers(),
                tcpConnection: trace?.TcpConnection ?? throw new Untraceable()
            );

            if (result is InfiniteLoop loop) throw loop;

            return (() => result, () => context.ResultFinalizer(result, context.Accept, context.OutputContentTypeProvider));
        }

        private static (Context, IResult) RunEvaluation(Methods method, ref string uri, byte[] body, Headers headers, TCPConnection tcpConnection)
        {
            if (StackDepth++ > 300) throw new InfiniteLoop();
            var o = (context: default(Context), result: default(IResult));

            try
            {
                switch (method)
                {
                    case GET:
                    case POST:
                    case PUT:
                    case PATCH:
                    case DELETE:
                    case REPORT:
                    case HEAD:
                        o.context = new Context(method, ref uri, body, headers, tcpConnection);
                        o.context.Authenticate();
                        o.context.ThrowIfError();
                        var requestedResource = o.context.IResource;
                        if (tcpConnection.HasWebSocket)
                        {
                            if (tcpConnection.Origin != OriginType.Shell
                                && requestedResource.Equals(EntityResource<AvailableResource>.Get)
                                && uri.Length < nameof(AvailableResource).Length)
                                requestedResource = Shell.TerminalResource;
                        }
                        switch (requestedResource)
                        {
                            case ITerminalResourceInternal terminal:
                                if (!tcpConnection.HasWebSocket)
                                {
                                    o.result = new UpgradeRequired(terminal.Name);
                                    return o;
                                }
                                terminal.InstantiateFor(tcpConnection.WebSocketInternal, o.context.Uri.Conditions);
                                o.result = new WebSocketResult(leaveOpen: true, trace: o.context);
                                return o;

                            case var entityResource:
                                o.result = HandleREST((dynamic) entityResource, o.context);
                                if (!tcpConnection.HasWebSocket || tcpConnection.Origin == OriginType.Shell)
                                    return o;
                                var finalized = o.context.ResultFinalizer(o.result, o.context.Accept, o.context.OutputContentTypeProvider);
                                tcpConnection.WebSocket.SendResult(finalized);
                                o.result = new WebSocketResult(leaveOpen: false, trace: o.context);
                                return o;
                        }
                    case OPTIONS:
                        o.context = new Context(method, ref uri, body, headers, tcpConnection);
                        o.context.ThrowIfError();
                        o.result = HandleOptions(o.context.IResource, o.context);
                        return o;

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
                    errorId = Trans(() => Error.Create(error, o.context)).Id;
                }
                if (tcpConnection.HasWebSocket && tcpConnection.Origin != OriginType.Shell)
                {
                    tcpConnection.WebSocket.SendResult(error);
                    o.result = new WebSocketResult(false, error);
                    return o;
                }
                switch (method)
                {
                    case GET:
                    case POST:
                    case PATCH:
                    case PUT:
                    case DELETE:
                    case REPORT:
                    case HEAD:
                        if (errorId != null)
                            error.Headers["ErrorInfo"] = $"/{typeof(Error).FullName}/id={HttpUtility.UrlEncode(errorId)}";
                        o.result = error;
                        return o;
                    case OPTIONS:
                        o.result = new InvalidOrigin();
                        return o;
                    // case VIEW:
                    // case PAGE:
                    // case MENU:
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
                if (!(o.result is WebSocketResult) && tcpConnection.Origin != OriginType.Shell)
                    NetworkConsole.Log(o.context, o.result);
                StackDepth--;
            }
        }

        private static Response HandleView<T>(IEntityResource<T> resource, Context context) where T : class
        {
            var request = new ViewRequest<T>(resource, context);
            // request.Authenticate();
            request.MethodCheck();
            request.Evaluate();
            return null; //request.GetView();
        }

        private static IResult HandleREST<T>(IEntityResource<T> resource, Context context)
            where T : class
        {
            using (var request = new RESTRequest<T>(resource, context))
            {
                request.RunResourceAuthentication();
                request.Evaluate();
                return request.Result;
            }
        }

        private static IResult HandleOptions(IResource resource, Context context)
        {
            var origin = context.Headers["Origin"];
            if (origin != null && origin != "null" && (AllowAllOrigins || AllowedOrigins.Contains(new Uri(origin))))
                return new AcceptOrigin(origin, resource, context);
            return new InvalidOrigin();
        }
    }
}