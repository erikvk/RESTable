using System.Text.RegularExpressions;
using ClosedXML.Excel;
using IResource = RESTar.Internal.IResource;

namespace RESTar.Requests
{
    internal struct Args
    {
        internal readonly string Resource;
        internal readonly string View;
        internal readonly string Conditions;
        internal readonly string MetaConditions;
        internal readonly bool HasResource;
        internal readonly bool HasView;
        internal readonly bool HasConditions;
        internal readonly bool HasMetaConditions;
        internal IResource IResource => HasResource ? RESTar.Resource.Find(Resource) : Resource<AvailableResource>.Get;
        private const string regex = @"\?*(?<resource>/[^/-]*)?(?<view>-\w*)?(?<conditions>/[^/]*)?(?<metaconditions>/[^/]*)?";

        internal Args(string query, bool escapePercentSigns = false)
        {
            if (query.CharCount('/') > 3) throw new InvalidSeparatorException();
            if (escapePercentSigns) query = query.Replace("%25", "%");
            var groups = Regex.Match(query, regex).Groups;
            var resource = groups["resource"].Value.TrimStart('/');
            var view = groups["view"].Value.TrimStart('-');
            var conditions = groups["conditions"].Value.TrimStart('/');
            var metaConditions = groups["metaconditions"].Value.TrimStart('/');
            HasResource = resource != "";
            HasView = view != "";
            HasConditions = conditions != "";
            HasMetaConditions = metaConditions != "";
            Resource = HasResource ? resource : null;
            View = HasView ? view : null;
            Conditions = HasConditions ? conditions : null;
            MetaConditions = HasMetaConditions ? metaConditions : null;
        }
    }
}