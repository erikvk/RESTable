using ClosedXML.Excel;
using RESTar.Admin;
using Starcounter;
using static RESTar.Internal.ErrorCodes;
using IResource = RESTar.Internal.IResource;

namespace RESTar.Requests
{
    internal struct Args
    {
        internal readonly string Resource;
        internal readonly string Conditions;
        internal readonly string MetaConditions;
        internal readonly bool HasResource;
        internal readonly bool HasConditions;
        internal readonly bool HasMetaConditions;
        internal IResource IResource => HasResource ? RESTar.Resource.Find(Resource) : Resource<AvailableResource>.Get;

        internal Args(string query, Request request)
        {
            Resource = Conditions = MetaConditions = null;
            HasResource = HasConditions = HasMetaConditions = false;
            query = query.TrimStart('?');
            if (string.IsNullOrEmpty(query) || query == "/") return;
            if (query.CharCount('/') > 3)
                throw new SyntaxException(InvalidSeparator,
                    "Invalid argument separator count. A RESTar URI can contain at most 3 " +
                    $"forward slashes after the base uri. URI scheme: {Settings._ResourcesPath}" +
                    "/[resource]/[conditions]/[meta-conditions]");
            if (request.HeadersDictionary.ContainsKey("X-ARR-LOG-ID"))
                query = query.Replace("%25", "%");
            if (query[0] == '/') query = query.Substring(1);
            var arr = query.Split('/');
            for (var i = 0; i < arr.Length; i++)
            {
                switch (i)
                {
                    case 0:
                        if (arr[i] != "")
                        {
                            Resource = arr[i];
                            HasResource = true;
                        }
                        break;
                    case 1:
                        if (arr[i] != "")
                        {
                            Conditions = arr[i];
                            HasConditions = true;
                        }
                        break;
                    case 2:
                        if (arr[i] != "")
                        {
                            MetaConditions = arr[i];
                            HasMetaConditions = true;
                        }
                        break;
                }
            }
        }
    }
}