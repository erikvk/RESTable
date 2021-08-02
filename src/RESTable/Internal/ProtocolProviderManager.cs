using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using RESTable.ContentTypeProviders;
using RESTable.DefaultProtocol;
using RESTable.Resources;
using RESTable.Results;

namespace RESTable.Internal
{
    public class ProtocolProviderManager
    {
        internal IDictionary<string, CachedProtocolProvider> CachedProtocolProviders { get; }
        public CachedProtocolProvider DefaultProtocolProvider { get; }
        private ContentTypeProviderManager ContentTypeProviderManager { get; }

        public ProtocolProviderManager(ContentTypeProviderManager contentTypeProviderManager, IEnumerable<IProtocolProvider> protocolProviders)
        {
            DefaultProtocolProvider = null!;
            CachedProtocolProviders = new Dictionary<string, CachedProtocolProvider>(StringComparer.OrdinalIgnoreCase);
            ContentTypeProviderManager = contentTypeProviderManager;
            foreach (var provider in protocolProviders)
            {
                ValidateProtocolProvider(provider);
                var cachedProvider = GetCachedProtocolProvider(provider);
                if (provider is DefaultProtocolProvider)
                {
                    DefaultProtocolProvider = cachedProvider;
                    CachedProtocolProviders[""] = cachedProvider;
                }
                var protocolId = provider.ProtocolIdentifier;
                if (CachedProtocolProviders.TryGetValue(protocolId, out CachedProtocolProvider existing))
                {
                    if (existing.GetType() == provider.GetType())
                        throw new InvalidProtocolProviderException(
                            $"A protocol provider of type '{existing.GetType()}' has already been added");
                    throw new InvalidProtocolProviderException(
                        $"Protocol identifier '{protocolId}' already claimed by a protocol provider of type '{existing.GetType()}'");
                }
                CachedProtocolProviders[protocolId] = cachedProvider;
            }
            if (!CachedProtocolProviders.Any())
                throw new InvalidOperationException("Expected at least one protocol provider available from the service provider given to RESTableConfig");
        }

        internal CachedProtocolProvider ResolveCachedProtocolProvider(string? protocolIdentifier) => protocolIdentifier is null
            ? DefaultProtocolProvider
            : CachedProtocolProviders.SafeGet(protocolIdentifier) ?? throw new UnknownProtocol(protocolIdentifier);

        private CachedProtocolProvider GetCachedProtocolProvider(IProtocolProvider provider)
        {
            var cProvider = new CachedProtocolProvider(provider);
            var contentTypeProviders = provider.GetCustomContentTypeProviders()?.ToList() ?? new List<IContentTypeProvider>();
            foreach (var contentTypeProvider in contentTypeProviders)
            {
                if (contentTypeProvider.CanRead)
                {
                    foreach (var mimeType in contentTypeProvider.MatchStrings)
                    {
                        cProvider.InputMimeBindings[mimeType] = contentTypeProvider;
                    }
                }
                if (contentTypeProvider.CanWrite)
                {
                    foreach (var mimeType in contentTypeProvider.MatchStrings)
                    {
                        cProvider.OutputMimeBindings[mimeType] = contentTypeProvider;
                    }
                }
            }
            switch (provider.ExternalContentTypeProviderSettings)
            {
                case ExternalContentTypeProviderSettings.AllowAll:
                    foreach (var pair in ContentTypeProviderManager.InputContentTypeProviders.Where(p => !cProvider.InputMimeBindings.ContainsKey(p.Key)))
                        cProvider.InputMimeBindings.Add(pair);
                    foreach (var pair in ContentTypeProviderManager.OutputContentTypeProviders.Where(p => !cProvider.OutputMimeBindings.ContainsKey(p.Key)))
                        cProvider.OutputMimeBindings.Add(pair);
                    break;
                case ExternalContentTypeProviderSettings.AllowInput:
                    foreach (var pair in ContentTypeProviderManager.InputContentTypeProviders.Where(p => !cProvider.InputMimeBindings.ContainsKey(p.Key)))
                        cProvider.InputMimeBindings.Add(pair);
                    break;
                case ExternalContentTypeProviderSettings.AllowOutput:
                    foreach (var pair in ContentTypeProviderManager.OutputContentTypeProviders.Where(p => !cProvider.OutputMimeBindings.ContainsKey(p.Key)))
                        cProvider.OutputMimeBindings.Add(pair);
                    break;
            }
            return cProvider;
        }

        private void ValidateProtocolProvider(IProtocolProvider provider)
        {
            if (provider is null)
                throw new InvalidProtocolProviderException("External protocol provider cannot be null");
            if (string.IsNullOrWhiteSpace(provider.ProtocolIdentifier))
                throw new InvalidProtocolProviderException($"Invalid protocol provider '{provider.GetType().GetRESTableTypeName()}'. " +
                                                           "ProtocolIdentifier cannot be null or whitespace");
            if (!Regex.IsMatch(provider.ProtocolIdentifier, "^[a-zA-Z]+$"))
                throw new InvalidProtocolProviderException($"Invalid protocol provider '{provider.GetType().GetRESTableTypeName()}'. " +
                                                           "ProtocolIdentifier can only contain letters a-z and A-Z");
            if (provider.ExternalContentTypeProviderSettings == ExternalContentTypeProviderSettings.DontAllow)
            {
                var contentProviders = provider.GetCustomContentTypeProviders()?.ToList();
                if (contentProviders?.Any() != true)
                    throw new InvalidProtocolProviderException($"Invalid protocol provider '{provider.GetType().GetRESTableTypeName()}'. " +
                                                               "The protocol provider allows no external content type providers " +
                                                               "and does not provide any content type providers of its own.");
                if (contentProviders.All(p => !p.CanRead) && contentProviders.All(p => !p.CanWrite))
                    throw new InvalidProtocolProviderException($"Invalid protocol provider '{provider.GetType().GetRESTableTypeName()}'. " +
                                                               "The protocol provider allows no external content type providers " +
                                                               "and none of the provided content type providers can read or write.");
            }
        }

        internal void OnInit()
        {
            foreach (var cachedProviders in CachedProtocolProviders.Values)
            {
                cachedProviders.ProtocolProvider.OnInit();
            }
        }
    }
}