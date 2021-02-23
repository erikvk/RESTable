using System;
using System.Collections.Generic;
using RESTable.ContentTypeProviders;
using RESTable.Resources;
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