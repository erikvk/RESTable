using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using RESTar.Admin;
using static System.Text.RegularExpressions.RegexOptions;
using IResource = RESTar.Internal.IResource;

namespace RESTar.Requests
{
    internal class Args
    {
        internal readonly string Resource;
        internal readonly string View;
        internal readonly string Conditions;
        internal string MetaConditions;
        internal readonly bool HasResource;
        internal readonly bool HasView;
        internal readonly bool HasConditions;
        internal readonly bool HasMetaConditions;
        internal IResource IResource => HasResource ? RESTar.Resource.Find(Resource) : Resource<AvailableResource>.Get;
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
                    total += Resource;
                else if (Macro != null)
                    total += "$" + Macro.Name;
                else total += "RESTar.AvailableResource";
                total += "/";
                if (HasConditions)
                    total += Conditions;
                total += "/";
                if (HasMetaConditions)
                    total += MetaConditions;
                return total.TrimEnd('/');
            }
        }

        /// <summary>
        /// Creates a new Args from a URI string, beginning after the base URI, for example /resource
        /// </summary>
        internal Args(string uriString, bool escapePercentSigns = false)
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
                    var macroArgs = new Args(Macro.Uri);
                    (HasResource, Resource) = (macroArgs.HasResource, macroArgs.Resource);
                    View = HasView ? view : macroArgs.View;
                    Conditions = macroArgs.HasConditions
                        ? (HasConditions ? $"{macroArgs.Conditions}&{conditions}" : macroArgs.Conditions)
                        : (HasConditions ? conditions : null);
                    MetaConditions = macroArgs.HasMetaConditions
                        ? (HasMetaConditions ? $"{macroArgs.MetaConditions}&{metaConditions}" : macroArgs.MetaConditions)
                        : (HasMetaConditions ? metaConditions : null);
                    HasConditions = Conditions != null;
                    HasMetaConditions = MetaConditions != null;
                }
                else
                {
                    HasResource = true;
                    Resource = resourceOrMacro;
                    View = HasView ? view : null;
                    Conditions = HasConditions ? conditions : null;
                    MetaConditions = HasMetaConditions ? metaConditions : null;
                }
            }
            else
            {
                View = HasView ? view : null;
                Conditions = HasConditions ? conditions : null;
                MetaConditions = HasMetaConditions ? metaConditions : null;
            }
        }
    }
}