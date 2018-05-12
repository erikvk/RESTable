using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using RESTar.Admin;
using RESTar.Internal;
using RESTar.ProtocolProviders;
using RESTar.Results;

namespace RESTar.Requests
{
    internal class UriComponents : IUriComponents
    {
        public string ResourceSpecifier { get; }
        public string ViewName { get; }
        public List<IUriCondition> Conditions { get; }
        public List<IUriCondition> MetaConditions { get; }
        public Func<IUriComponents, string> StringMaker { get; }

        IEnumerable<IUriCondition> IUriComponents.Conditions => Conditions;
        IEnumerable<IUriCondition> IUriComponents.MetaConditions => MetaConditions;

        public UriComponents(string resourceSpecifier, string viewName, IEnumerable<IUriCondition> conditions,
            IEnumerable<IUriCondition> metaConditions, IProtocolProvider protocolProvider)
        {
            ResourceSpecifier = resourceSpecifier;
            ViewName = viewName;
            Conditions = conditions.ToList();
            MetaConditions = metaConditions.ToList();
            StringMaker = protocolProvider.MakeRelativeUri;
        }

        public UriComponents(IUriComponents existing)
        {
            ResourceSpecifier = existing.ResourceSpecifier;
            ViewName = existing.ViewName;
            Conditions = existing.Conditions.ToList();
            MetaConditions = existing.MetaConditions.ToList();
            StringMaker = existing.StringMaker;
        }

        public override string ToString() => StringMaker(this);
    }

    /// <inheritdoc />
    /// <summary>
    /// Encodes a URI that is used in a request
    /// </summary>
    public class URI : IUriComponents
    {
        /// <inheritdoc />
        public string ResourceSpecifier { get; set; }

        /// <inheritdoc />
        public string ViewName { get; set; }

        IEnumerable<IUriCondition> IUriComponents.Conditions => Conditions.Cast<IUriCondition>();
        IEnumerable<IUriCondition> IUriComponents.MetaConditions => MetaConditions.Cast<IUriCondition>();

        /// <summary>
        /// The conditions contained in the URI
        /// </summary>
        public List<UriCondition> Conditions { get; }

        /// <summary>
        /// The MetaConditions contained in the URI
        /// </summary>
        public List<UriCondition> MetaConditions { get; }

        internal DbMacro Macro { get; set; }
        internal Exception Error { get; private set; }
        internal bool HasError => Error != null;
        private IProtocolProvider ProtocolProvider { get; set; }

        internal static URI ParseInternal(string uriString, bool percentCharsEscaped, Context context,
            out CachedProtocolProvider cachedProtocolProvider)
        {
            var uri = new URI();
            if (percentCharsEscaped) uriString = uriString.Replace("%25", "%");
            var groups = Regex.Match(uriString, RegEx.Protocol).Groups;
            var protocolString = groups["proto"].Value;
            var tail = groups["tail"].Value;
            if (!ProtocolController.ProtocolProviders.TryGetValue(protocolString, out cachedProtocolProvider))
            {
                uri.Error = new UnknownProtocol(protocolString);
                return uri;
            }
            uri.ProtocolProvider = cachedProtocolProvider.ProtocolProvider;
            try
            {
                cachedProtocolProvider.ProtocolProvider.PopulateURI(tail, uri, context);
            }
            catch (Exception e)
            {
                uri.Error = e;
            }
            return uri;
        }

        internal static URI Parse(string uriString)
        {
            var context = new InternalContext();
            var uri = ParseInternal(uriString, false, context, out _);
            if (uri.HasError) throw uri.Error;
            return uri;
        }

        internal URI()
        {
            Conditions = new List<UriCondition>();
            MetaConditions = new List<UriCondition>();
            StringMaker = c =>
            {
                var provider = ProtocolProvider ?? ProtocolController.DefaultProtocolProvider.ProtocolProvider;
                return provider.MakeRelativeUri(c);
            };
        }

        /// <inheritdoc />
        public Func<IUriComponents, string> StringMaker { get; }

        /// <inheritdoc />
        public override string ToString() => StringMaker(this);
    }
}