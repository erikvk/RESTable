using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using RESTable.Internal;
using RESTable.ProtocolProviders;
using RESTable.Results;

namespace RESTable.Requests
{
    /// <inheritdoc />
    /// <summary>
    /// Encodes a URI that is used in a request
    /// </summary>
    internal class URI : IUriComponents
    {
        public string ProtocolIdentifier { get; private set; }

        /// <inheritdoc />
        public string? ResourceSpecifier { get; private set; }

        /// <inheritdoc />
        public string? ViewName { get; private set; }

        /// <inheritdoc />
        public IReadOnlyCollection<IUriCondition> Conditions { get; private set; }

        /// <inheritdoc />
        public IReadOnlyCollection<IUriCondition> MetaConditions { get; private set; }

        public IMacro? Macro { get; private set; }

        internal Exception Error { get; private set; }
        internal bool HasError => Error is not null;

        public IProtocolProvider ProtocolProvider { get; set; }

        internal static URI ParseInternal
        (
            string uriString,
            bool percentCharsEscaped,
            RESTableContext context,
            out CachedProtocolProvider cachedProtocolProvider
        )
        {
            var uri = new URI();
            if (percentCharsEscaped) uriString = uriString.Replace("%25", "%");
            var groups = Regex.Match(uriString, RegEx.Protocol).Groups;
            var protocolString = groups["proto"].Value;
            if (protocolString.StartsWith("-"))
                protocolString = protocolString.Substring(1);
            var tail = groups["tail"].Value;
            uri.ProtocolIdentifier = protocolString.ToLowerInvariant();
            var protocolProviders = context.GetRequiredService<ProtocolProviderManager>().CachedProtocolProviders;
            if (!protocolProviders.TryGetValue(protocolString, out cachedProtocolProvider))
            {
                uri.Error = new UnknownProtocol(protocolString);
                return uri;
            }
            uri.ProtocolProvider = cachedProtocolProvider.ProtocolProvider;
            try
            {
                uri.Populate(cachedProtocolProvider.ProtocolProvider.GetUriComponents(tail, context));
            }
            catch (Exception e)
            {
                uri.Error = e;
            }
            return uri;
        }

        private void Populate(IUriComponents components)
        {
            ResourceSpecifier = components.ResourceSpecifier;
            ViewName = components.ViewName;
            Conditions = components.Conditions;
            MetaConditions = components.MetaConditions;
            Macro = components.Macro;
        }

        private URI()
        {
            Conditions = new List<IUriCondition>();
            MetaConditions = new List<IUriCondition>();
        }

        internal URI(string resourceSpecifier, string viewName) : this()
        {
            ResourceSpecifier = resourceSpecifier;
            ViewName = viewName;
        }

        /// <inheritdoc />
        public override string ToString() => this.ToUriString();
    }
}