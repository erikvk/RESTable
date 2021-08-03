using System;
using System.Collections.Generic;
using System.Linq;
using RESTable.ContentTypeProviders;
using RESTable.Resources;

namespace RESTable.Internal
{
    public class ContentTypeProviderManager
    {
        internal IDictionary<string, IContentTypeProvider> InputContentTypeProviders { get; }
        internal IDictionary<string, IContentTypeProvider> OutputContentTypeProviders { get; }

        public ContentTypeProviderManager(IEnumerable<IContentTypeProvider> contentTypeProviders)
        {
            InputContentTypeProviders = new Dictionary<string, IContentTypeProvider>(StringComparer.OrdinalIgnoreCase);
            OutputContentTypeProviders = new Dictionary<string, IContentTypeProvider>(StringComparer.OrdinalIgnoreCase);

            var contentTypeProvidersList = contentTypeProviders.ToList();

            foreach (var provider in contentTypeProvidersList)
            {
                ValidateContentTypeProvider(provider);
                if (provider.CanRead)
                {
                    foreach (var mimeType in provider.MatchStrings)
                    {
                        InputContentTypeProviders[mimeType] = provider;
                    }
                }
                if (provider.CanWrite)
                {
                    foreach (var mimeType in provider.MatchStrings)
                    {
                        OutputContentTypeProviders[mimeType] = provider;
                    }
                }
            }
        }

        private void ValidateContentTypeProvider(IContentTypeProvider provider)
        {
            if (provider is null)
                throw new InvalidContentTypeProviderException("External content type provider cannot be null");
            if (!provider.CanRead && !provider.CanWrite)
                throw new InvalidContentTypeProviderException($"Content type provider '{provider.GetType().GetRESTableTypeName()}' can't read nor write. A content type provider " +
                                                              "must be able to either read or write.");
        }
    }
}