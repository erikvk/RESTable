using System;
using System.Collections.Generic;
using System.Linq;
using RESTable.ContentTypeProviders;
using RESTable.Resources;
using RESTable.Linq;

namespace RESTable.Internal
{
    public class ContentTypeController
    {
        internal IDictionary<string, IContentTypeProvider> InputContentTypeProviders { get; }
        internal IDictionary<string, IContentTypeProvider> OutputContentTypeProviders { get; }

        public ContentTypeController(IEnumerable<IContentTypeProvider> contentTypeProviders)
        {
            InputContentTypeProviders = new Dictionary<string, IContentTypeProvider>(StringComparer.OrdinalIgnoreCase);
            OutputContentTypeProviders = new Dictionary<string, IContentTypeProvider>(StringComparer.OrdinalIgnoreCase);

            var contentTypeProvidersList = contentTypeProviders.ToList();
            
            foreach (var provider in contentTypeProvidersList)
            {
                ValidateContentTypeProvider(provider);
                if (provider.CanRead)
                    provider.MatchStrings?.ForEach(mimeType => InputContentTypeProviders[mimeType] = provider);
                if (provider.CanWrite)
                    provider.MatchStrings?.ForEach(mimeType => OutputContentTypeProviders[mimeType] = provider);
            }            
        }

        private void ValidateContentTypeProvider(IContentTypeProvider provider)
        {
            if (provider == null)
                throw new InvalidContentTypeProviderException("External content type provider cannot be null");
            if (!provider.CanRead && !provider.CanWrite)
                throw new InvalidContentTypeProviderException($"Provider '{provider.GetType().GetRESTableTypeName()}' cannot read or write");
        }
    }
}