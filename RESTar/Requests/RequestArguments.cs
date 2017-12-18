using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using RESTar.Admin;
using static System.Text.RegularExpressions.RegexOptions;
using IResource = RESTar.Internal.IResource;

namespace RESTar.Requests
{
    internal struct UriCondition
    {
        internal string Key { get; }
        internal Operator Operator { get; }
        internal string ValueLiteral { get; }
        public override string ToString() => $"{Key}{Operator.Common}{ValueLiteral}";
        public UriCondition(string key, Operator op, string literal) => (Key, Operator, ValueLiteral) = (key, op, literal);
    }

    internal class RequestArguments
    {
        internal string ResourceSpecifier { get; }
        internal string ViewName { get; }
        internal List<UriCondition> UriConditions { get; }
        internal List<UriCondition> UriMetaConditions { get; set; }

        internal bool HasResource { get; }
        internal bool HasView { get; }
        internal bool HasConditions { get; }
        internal bool HasMetaConditions { get; }

        internal IResource IResource => HasResource ? Resource.Find(ResourceSpecifier) : Resource<AvailableResource>.Get;
        internal DbMacro Macro { get; }
        internal Origin Origin { get; set; }
        internal byte[] BodyBytes { get; set; }
        internal IDictionary<string, string> Headers { get; set; }
        internal string ContentType { get; set; }
        internal string Accept { get; set; }

        internal IEnumerable<KeyValuePair<string, string>> NonReservedHeaders =>
            Headers.Where(h => !Regex.IsMatch(h.Key, RegEx.ReservedHeaders, IgnoreCase));

        internal string UriString
        {
            get
            {
                var total = "/";
                if (HasResource)
                    total += ResourceSpecifier;
                else if (Macro != null)
                    total += "$" + Macro.Name;
                else total += "RESTar.AvailableResource";
                total += "/";
                if (HasConditions)
                    total += string.Join("$", UriConditions);
                total += "/";
                if (HasMetaConditions)
                    total += string.Join("$", UriMetaConditions);
                return total.TrimEnd('/');
            }
        }

        /// <summary>
        /// Creates a new Args from a URI string, beginning after the base URI, for example /resource
        /// </summary>
        internal RequestArguments(string uriString, bool escapePercentSigns = false)
        {
            Headers = new Dictionary<string, string>();
            if (uriString.CharCount('/') > 3) throw new InvalidSeparatorException();
            if (escapePercentSigns) uriString = uriString.Replace("%25", "%");
            var uriGroups = Regex.Match(uriString, RegEx.RequestUri).Groups;
            var resourceOrMacro = uriGroups["resource_or_macro"].Value.TrimStart('/');
            var view = uriGroups["view"].Value.TrimStart('-');
            var conditions = uriGroups["conditions"].Value.TrimStart('/');
            var metaConditions = uriGroups["metaconditions"].Value.TrimStart('/');
            HasView = view != "";
            HasConditions = conditions != "";
            HasMetaConditions = metaConditions != "";
            var hasResourceOrMacro = resourceOrMacro != "";
            if (hasResourceOrMacro)
            {
                var hasMacro = resourceOrMacro[0] == '$';
                if (hasMacro)
                {
                    var macroString = resourceOrMacro.Substring(1);
                    Macro = DbMacro.Get(macroString) ?? throw new UnknownMacroException(macroString);
                    var macroArgs = new RequestArguments(Macro.Uri);
                    (HasResource, ResourceSpecifier) = (macroArgs.HasResource, macroArgs.ResourceSpecifier);
                    ViewName = HasView ? view : macroArgs.ViewName;
                    UriConditions = macroArgs.HasConditions
                        ? (HasConditions ? $"{macroArgs.UriConditions}&{conditions}" : macroArgs.UriConditions)
                        : (HasConditions ? conditions : null);
                    UriMetaConditions = macroArgs.HasMetaConditions
                        ? (HasMetaConditions ? $"{macroArgs.UriMetaConditions}&{metaConditions}" : macroArgs.UriMetaConditions)
                        : (HasMetaConditions ? metaConditions : null);
                    HasConditions = UriConditions != null;
                    HasMetaConditions = UriMetaConditions != null;
                }
                else
                {
                    HasResource = true;
                    ResourceSpecifier = resourceOrMacro;
                    ViewName = HasView ? view : null;
                    UriConditions = HasConditions ? conditions : null;
                    UriMetaConditions = HasMetaConditions ? metaConditions : null;
                }
            }
            else
            {
                ViewName = HasView ? view : null;
                UriConditions = HasConditions ? conditions : null;
                UriMetaConditions = HasMetaConditions ? metaConditions : null;
            }
        }
    }
}