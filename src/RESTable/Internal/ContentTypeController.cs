using System;
using System.Collections.Generic;
using System.Linq;
using RESTable.ContentTypeProviders;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Results;
using RESTable.Linq;

namespace RESTable.Internal
{
    internal static class ContentTypeController
    {
        internal static IDictionary<string, IContentTypeProvider> InputContentTypeProviders { get; private set; }
        internal static IDictionary<string, IContentTypeProvider> OutputContentTypeProviders { get; private set; }

        private static void ValidateContentTypeProvider(IContentTypeProvider provider)
        {
            if (provider == null)
                throw new InvalidContentTypeProviderException("External content type provider cannot be null");
            if (!provider.CanRead && !provider.CanWrite)
                throw new InvalidContentTypeProviderException($"Provider '{provider.GetType().GetRESTableTypeName()}' cannot read or write");
        }

        internal static ContentType ResolveInputContentType(IRequestInternal request = null, ContentType? contentType = null)
        {
            IContentTypeProvider provider;
            if (request != null)
            {
                provider = request.GetInputContentTypeProvider(contentType);
                request.Headers.ContentType = provider.ContentType;
                return provider.ContentType;
            }
            if (contentType.HasValue)
            {
                if (InputContentTypeProviders.TryGetValue(contentType.Value.ToString(), out provider))
                    return provider.ContentType;
                throw new UnsupportedContent(contentType.ToString(), false);
            }
            return default;
        }

        internal static IContentTypeProvider GetInputContentTypeProvider(this IProtocolHolder protocolHolder, ContentType? contentTypeOverride = null)
        {
            var contentType = contentTypeOverride ?? protocolHolder.Headers.ContentType ?? protocolHolder.CachedProtocolProvider.DefaultInputProvider.ContentType;
            if (!protocolHolder.CachedProtocolProvider.InputMimeBindings.TryGetValue(contentType.MediaType, out var contentTypeProvider))
                throw new UnsupportedContent(contentType.ToString());
            return contentTypeProvider;
        }

        internal static IContentTypeProvider GetOutputContentTypeProvider(this IProtocolHolder protocolHolder, ContentType? contentTypeOverride = null)
        {
            IContentTypeProvider acceptProvider = null;

            var protocolProvider = protocolHolder.CachedProtocolProvider;
            var headers = protocolHolder.Headers;
            var contentType = contentTypeOverride;
            if (contentType.HasValue)
                contentType = contentType.Value;
            else if (!(headers.Accept?.Count > 0))
                contentType = protocolProvider.DefaultOutputProvider.ContentType;
            if (!contentType.HasValue)
            {
                var containedWildcard = false;
                var foundProvider = headers.Accept.Any(a =>
                {
                    if (!a.AnyType)
                        return protocolProvider.OutputMimeBindings.TryGetValue(a.MediaType, out acceptProvider);
                    containedWildcard = true;
                    return false;
                });
                if (!foundProvider)
                    if (containedWildcard)
                        acceptProvider = protocolProvider.DefaultOutputProvider;
                    else
                        throw new NotAcceptable(headers.Accept.ToString());
            }
            else if (!protocolProvider.OutputMimeBindings.TryGetValue(contentType.Value.MediaType, out acceptProvider))
                throw new NotAcceptable(contentType.Value.ToString());
            return acceptProvider;
        }

        internal static void SetupContentTypeProviders(List<IContentTypeProvider> contentTypeProviders)
        {
            InputContentTypeProviders = new Dictionary<string, IContentTypeProvider>(StringComparer.OrdinalIgnoreCase);
            OutputContentTypeProviders = new Dictionary<string, IContentTypeProvider>(StringComparer.OrdinalIgnoreCase);
            contentTypeProviders ??= new List<IContentTypeProvider>();
            contentTypeProviders.Insert(0, new XMLWriter());
            contentTypeProviders.Insert(0, Providers.Json);
            foreach (var provider in contentTypeProviders)
            {
                ValidateContentTypeProvider(provider);
                if (provider.CanRead)
                    provider.MatchStrings?.ForEach(mimeType => InputContentTypeProviders[mimeType] = provider);
                if (provider.CanWrite)
                    provider.MatchStrings?.ForEach(mimeType => OutputContentTypeProviders[mimeType] = provider);
            }
        }
    }
}