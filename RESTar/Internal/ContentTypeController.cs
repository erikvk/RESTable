using System;
using System.Collections.Generic;
using System.Linq;
using RESTar.ContentTypeProviders;
using RESTar.Linq;
using RESTar.Results;
using RESTar.Serialization;

namespace RESTar.Internal
{
    internal static class ContentTypeController
    {
        internal static IDictionary<string, IContentTypeProvider> InputContentTypeProviders { get; set; }
        internal static IDictionary<string, IContentTypeProvider> OutputContentTypeProviders { get; set; }

        private static void ValidateContentTypeProvider(IContentTypeProvider provider)
        {
            if (provider == null)
                throw new InvalidContentTypeProviderException("External content type provider cannot be null");
            if (!provider.CanRead && !provider.CanWrite)
                throw new InvalidContentTypeProviderException($"Provider '{provider.GetType().RESTarTypeName()}' cannot read or write");
        }

        internal static IContentTypeProvider ResolveInputContentTypeProvider(IRequestInternal request, ContentType? providedContentType)
        {
            providedContentType = providedContentType ?? request.Headers.ContentType ?? request.CachedProtocolProvider.DefaultInputProvider.ContentType;
            if (!request.CachedProtocolProvider.InputMimeBindings.TryGetValue(providedContentType.Value.MediaType, out var contentTypeProvider))
                throw new UnsupportedContent(providedContentType.ToString());
            return contentTypeProvider;
        }

        internal static IContentTypeProvider ResolveOutputContentTypeProvider(IRequestInternal request, ContentType? providedContentType)
        {
            var protocolProvider = request.CachedProtocolProvider;
            if (providedContentType.HasValue)
                providedContentType = providedContentType.Value;
            else if (!(request.Headers.Accept?.Count > 0))
                providedContentType = protocolProvider.DefaultOutputProvider.ContentType;
            IContentTypeProvider acceptProvider = null;
            if (!providedContentType.HasValue)
            {
                var containedWildcard = false;
                var foundProvider = request.Headers.Accept.Any(a =>
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
                        throw new NotAcceptable(request.Headers.Accept.ToString());
            }
            else if (!protocolProvider.OutputMimeBindings.TryGetValue(providedContentType.Value.MediaType, out acceptProvider))
                throw new NotAcceptable(providedContentType.Value.ToString());
            return acceptProvider;
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
    }
}