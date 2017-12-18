using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using RESTar.Admin;
using static System.Text.RegularExpressions.RegexOptions;
using IResource = RESTar.Internal.IResource;

namespace RESTar.Requests
{
    internal class RequestArguments
    {
        internal string ResourceSpecifier { get; set; }
        internal string ViewName { get; set; }
        internal List<UriCondition> UriConditions { get; }
        internal List<UriCondition> UriMetaConditions { get; set; }
        internal IResource IResource => Resource.Find(ResourceSpecifier);
        internal DbMacro Macro { get; set; }
        internal Origin Origin { get; set; }
        internal byte[] BodyBytes { get; set; }
        internal IDictionary<string, string> Headers { get; set; }
        internal string ContentType { get; set; }
        internal string Accept { get; set; }

        internal IEnumerable<KeyValuePair<string, string>> NonReservedHeaders =>
            Headers.Where(h => !Regex.IsMatch(h.Key, RegEx.ReservedHeaders, IgnoreCase));

        private static readonly string DefaultResourceSpecifier = typeof(AvailableResource).FullName;

        internal string UriString
        {
            get
            {
                var total = "/";
                total += Macro != null ? "$" + Macro.Name : ResourceSpecifier;
                total += "/";
                total += UriConditions != null ? string.Join("$", UriConditions) : null;
                total += "/";
                total += UriMetaConditions != null ? string.Join("$", UriMetaConditions) : null;
                return total.TrimEnd('/');
            }
        }

        internal RequestArguments()
        {
            UriConditions = new List<UriCondition>();
            UriMetaConditions = new List<UriCondition>();
            ResourceSpecifier = DefaultResourceSpecifier;
        }

        /// <summary>
        /// Creates a new Args from a standard RESTar URI string, beginning after the base 
        /// URI, for example /resource
        /// </summary>
        internal RequestArguments(string uriString, bool escapePercentSigns = false) : this()
        {
            Headers = new Dictionary<string, string>();
            if (uriString.Count(c => c == '/') > 3) throw new InvalidSeparatorException();
            if (escapePercentSigns) uriString = uriString.Replace("%25", "%");
            var uriGroups = Regex.Match(uriString, RegEx.RESTarRequestUri).Groups;
            var resourceOrMacro = uriGroups["resource_or_macro"].Value.TrimStart('/');
            var view = uriGroups["view"].Value.TrimStart('-');
            var conditions = uriGroups["conditions"].Value.TrimStart('/');
            var metaConditions = uriGroups["metaconditions"].Value.TrimStart('/');
            if (conditions.Any())
                UriConditions.AddRange(UriCondition.ParseMany(conditions, true));
            if (metaConditions.Any())
                UriMetaConditions.AddRange(UriCondition.ParseMany(metaConditions, true));
            if (view.Any())
                ViewName = view;
            if (resourceOrMacro != "")
            {
                if (resourceOrMacro[0] != '$')
                    ResourceSpecifier = resourceOrMacro;
                else
                {
                    var macroString = resourceOrMacro.Substring(1);
                    Macro = DbMacro.Get(macroString) ?? throw new UnknownMacroException(macroString);
                    ResourceSpecifier = Macro.ResourceSpecifier;
                    ViewName = ViewName ?? Macro.ViewName;
                    var macroConditions = Macro.UriConditions;
                    if (macroConditions != null)
                        UriConditions.AddRange(macroConditions);
                    var macroMetaConditions = Macro.UriMetaConditions;
                    if (macroMetaConditions != null)
                        UriMetaConditions.AddRange(macroMetaConditions);
                }
            }
        }
    }
}

