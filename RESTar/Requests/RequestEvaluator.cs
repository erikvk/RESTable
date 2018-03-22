﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using RESTar.ContentTypeProviders;
using RESTar.Linq;
using RESTar.Results.Error;
using RESTar.Serialization;

namespace RESTar.Requests
{
    internal class CachedProtocolProvider
    {
        internal IProtocolProvider ProtocolProvider { get; }
        internal IDictionary<string, IContentTypeProvider> InputMimeBindings { get; }
        internal IDictionary<string, IContentTypeProvider> OutputMimeBindings { get; }
        internal IContentTypeProvider DefaultInputProvider => InputMimeBindings.FirstOrDefault().Value;
        internal IContentTypeProvider DefaultOutputProvider => OutputMimeBindings.FirstOrDefault().Value;

        public CachedProtocolProvider(IProtocolProvider protocolProvider)
        {
            ProtocolProvider = protocolProvider;
            InputMimeBindings = new Dictionary<string, IContentTypeProvider>(StringComparer.OrdinalIgnoreCase);
            OutputMimeBindings = new Dictionary<string, IContentTypeProvider>(StringComparer.OrdinalIgnoreCase);
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
        internal static CachedProtocolProvider DefaultProtocolProvider { get; private set; }

        private static CachedProtocolProvider GetCachedProtocolProvider(IProtocolProvider provider)
        {
            var cProvider = new CachedProtocolProvider(provider);
            var contentTypeProviders = provider.GetContentTypeProviders()?.ToList();
            contentTypeProviders?.ForEach(contentTypeProvider =>
            {
                if (contentTypeProvider.CanRead)
                    contentTypeProvider.MatchStrings?.ForEach(mimeType => cProvider.InputMimeBindings[mimeType] = contentTypeProvider);
                if (contentTypeProvider.CanWrite)
                    contentTypeProvider.MatchStrings?.ForEach(mimeType => cProvider.OutputMimeBindings[mimeType] = contentTypeProvider);
            });
            if (!provider.AllowExternalContentProviders) return cProvider;
            InputContentTypeProviders.Where(p => !cProvider.InputMimeBindings.ContainsKey(p.Key)).ForEach(cProvider.InputMimeBindings.Add);
            OutputContentTypeProviders.Where(p => !cProvider.OutputMimeBindings.ContainsKey(p.Key)).ForEach(cProvider.OutputMimeBindings.Add);
            return cProvider;
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
                if (contentProviders.All(p => !p.CanRead) && contentProviders.All(p => !p.CanWrite))
                    throw new InvalidProtocolProvider($"Invalid protocol provider '{provider.GetType().RESTarTypeName()}'. " +
                                                      "The protocol provider allows no external content type providers " +
                                                      "and none of the provided content type providers can read or write.");
            }
        }

        private static void ValidateContentTypeProvider(IContentTypeProvider provider)
        {
            if (provider == null)
                throw new InvalidContentTypeProvider("External content type provider cannot be null");
            if (!provider.CanRead && !provider.CanWrite)
                throw new InvalidContentTypeProvider($"Provider '{provider.GetType().RESTarTypeName()}' cannot read or write");
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
                if (provider.CanRead)
                    provider.MatchStrings?.ForEach(mimeType => InputContentTypeProviders[mimeType] = provider);
                if (provider.CanWrite)
                    provider.MatchStrings?.ForEach(mimeType => OutputContentTypeProviders[mimeType] = provider);
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
                {
                    DefaultProtocolProvider = cachedProvider;
                    ProtocolProviders[""] = cachedProvider;
                }
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
        // public static (Func<IResult> GetRawResult, Func<IFinalizedResult> GetFinalizedResult) Evaluate(ITraceable trace, Methods method, ref string uri,
        //     byte[] body = null, Headers headers = null)
        // {
        //     if (uri == null) throw new MissingUri();
        //     var stopwatch = Stopwatch.StartNew();
        //     var tcpConnection = trace?.Client ?? throw new Untraceable();
        // 
        //     var result = RunEvaluation
        //     (
        //         method: method,
        //         uri: ref uri,
        //         body: body,
        //         headers: headers ?? new Headers(),
        //         client: tcpConnection,
        //         isWebSocketUpgrade: out var isWebSocketUpgrade,
        //         requestParameters: out var context
        //     );
        // 
        //     if (result is InfiniteLoop loop) throw loop;
        //     var logged = false;
        // 
        //     void post(ILogable tolog)
        //     {
        //         if (tcpConnection.StackDepth == 0 && !tcpConnection.HasWebSocket)
        //             tcpConnection.Dispose();
        //         stopwatch.Stop();
        //         if (logged) return;
        //         if (!trace.Client.IsInShell)
        //             Console.Log(context, tolog, stopwatch.Elapsed.TotalMilliseconds);
        //         logged = true;
        //     }
        // 
        //     return
        //     (
        //         GetRawResult: () =>
        //         {
        //             post(result);
        //             return result;
        //         },
        //         GetFinalizedResult: () =>
        //         {
        //             IFinalizedResult finalized;
        //             try
        //             {
        //                 finalized = context.ResultFinalizer(result, context.OutputContentTypeProvider);
        //             }
        //             catch (Exception exs)
        //             {
        //                 finalized = RESTarError.GetResult(exs, method, context, trace.Client, isWebSocketUpgrade);
        //             }
        //             post(finalized);
        //             return finalized;
        //         }
        //     );
        // }

        // private static IResult RunEvaluation(Methods method, ref string uri, byte[] body, Headers headers, Client client,
        //     out bool isWebSocketUpgrade, out RequestParameters requestParameters)
        // {
        //     client.StackDepth += 1;
        //     if (client.StackDepth > 300) throw new InfiniteLoop();
        //     requestParameters = null;
        //     isWebSocketUpgrade = client.IsWebSocketUpgrade;
        // 
        //     try
        //     {
        //         switch (method)
        //         {
        //             case GET:
        //             case POST:
        //             case PUT:
        //             case PATCH:
        //             case DELETE:
        //             case REPORT:
        //             case HEAD:
        //                 requestParameters = new RequestParameters(method, ref uri, body, headers, client);
        //                 requestParameters.Authenticate();
        //                 requestParameters.ThrowIfError();
        //                 switch (requestParameters.IResource)
        //                 {
        //                     case ITerminalResourceInternal terminal:
        //                         if (!client.HasWebSocket)
        //                             return new UpgradeRequired(terminal.Name);
        //                         terminal.InstantiateFor(client.WebSocketInternal, requestParameters.Uri.Conditions);
        //                         return new WebSocketResult(leaveOpen: true, trace: requestParameters);
        //                     case var entityResource:
        //                         var result = HandleREST((dynamic) entityResource, requestParameters);
        //                         if (!isWebSocketUpgrade) return result;
        //                         var finalized = requestParameters.ResultFinalizer(result, requestParameters.OutputContentTypeProvider);
        //                         client.WebSocket.SendResult(finalized);
        //                         return new WebSocketResult(leaveOpen: false, trace: requestParameters);
        //                 }
        //             case OPTIONS:
        //                 requestParameters = new RequestParameters(method, ref uri, body, headers, client);
        //                 requestParameters.ThrowIfError();
        //                 return HandleOptions(requestParameters.IResource, requestParameters);
        //             default: throw new ArgumentOutOfRangeException(nameof(method), method, null);
        //         }
        //     }
        //     catch (Exception exs)
        //     {
        //         return RESTarError.GetResult(exs, method, requestParameters, client, isWebSocketUpgrade);
        //     }
        //     finally
        //     {
        //         client.StackDepth -= 1;
        //     }
        // }
        // 
        // private static IResult HandleREST<T>(IEntityResource<T> resource, RequestParameters requestParameters) where T : class
        // {
        //     var request = new RESTRequest<T>(resource, requestParameters);
        //     request.RunResourceAuthentication();
        //     request.Evaluate();
        //     return request.Result;
        // }
        // 
        // private static IResult HandleOptions(IResource resource, RequestParameters requestParameters)
        // {
        //     var origin = requestParameters.Headers["Origin"];
        //     if (origin != null && origin != "null" && (AllowAllOrigins || AllowedOrigins.Contains(new Uri(origin))))
        //         return new AcceptOrigin(origin, resource, requestParameters);
        //     return new InvalidOrigin();
        // }
    }
}