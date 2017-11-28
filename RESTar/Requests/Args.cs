using System.Text.RegularExpressions;
using ClosedXML.Excel;
using RESTar.Admin;
using IResource = RESTar.Internal.IResource;

namespace RESTar.Requests
{
    internal struct Args
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

        public override string ToString()
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


        internal Args(string query, bool escapePercentSigns = false)
        {
            if (query.CharCount('/') > 3) throw new InvalidSeparatorException();
            if (escapePercentSigns) query = query.Replace("%25", "%");
            var uriGroups = Regex.Match(query, RegEx.RequestUri).Groups;
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

                    // Use macro's resource
                    (HasResource, Resource) = (macroArgs.HasResource, macroArgs.Resource);

                    // Use macro's view if no is included in caller
                    View = HasView ? view : macroArgs.View;

                    // Concatenate caller conditions with the macro's
                    Conditions = macroArgs.HasConditions
                        ? (HasConditions ? $"{macroArgs.Conditions}&{conditions}" : macroArgs.Conditions)
                        : (HasConditions ? conditions : null);

                    // Concatenate caller meta-conditions with the macro's
                    MetaConditions = macroArgs.HasMetaConditions
                        ? (HasMetaConditions ? $"{macroArgs.MetaConditions}&{metaConditions}" : macroArgs.MetaConditions)
                        : (HasMetaConditions ? metaConditions : null);

                    HasConditions = Conditions != null;
                    HasMetaConditions = MetaConditions != null;
                }
                else
                {
                    Macro = null;
                    HasResource = true;
                    Resource = resourceOrMacro;
                    View = HasView ? view : null;
                    Conditions = HasConditions ? conditions : null;
                    MetaConditions = HasMetaConditions ? metaConditions : null;
                }
            }
            else
            {
                Macro = null;
                HasResource = false;
                Resource = null;
                View = HasView ? view : null;
                Conditions = HasConditions ? conditions : null;
                MetaConditions = HasMetaConditions ? metaConditions : null;
            }
        }
    }
}