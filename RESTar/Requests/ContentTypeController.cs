using System;
using System.Collections.Generic;
using RESTar.ContentTypeProviders;
using RESTar.Linq;
using RESTar.Results.Error;
using RESTar.Serialization;

namespace RESTar.Queries
{
    internal static class ContentTypeController
    {
        internal static IDictionary<string, IContentTypeProvider> InputContentTypeProviders { get; set; }
        internal static IDictionary<string, IContentTypeProvider> OutputContentTypeProviders { get; set; }

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
    }
}