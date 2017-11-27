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
            if (resourceOrMacro != "")
            {
                if (resourceOrMacro[0] == '$')
                {
                    var macroString = resourceOrMacro.Substring(1);
                    Macro = DbMacro.Get(macroString);
                    if (Macro == null) throw new UnknownMacroException(macroString);
                    var innerArgs = new Args(Macro.Uri);
                    HasResource = innerArgs.HasResource;
                    Resource = innerArgs.Resource;
                    View = HasView ? view : innerArgs.View;
                    Conditions = innerArgs.HasConditions
                        ? (HasConditions ? $"{innerArgs.Conditions}&{conditions}" : innerArgs.Conditions)
                        : (HasConditions ? conditions : null);
                    MetaConditions = innerArgs.HasMetaConditions
                        ? (HasMetaConditions ? $"{innerArgs.MetaConditions}&{metaConditions}" : innerArgs.MetaConditions)
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