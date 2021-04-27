using System.Collections.Generic;

namespace RESTable
{
    public class AllowedCorsOrigins : List<string> { }

    public class ApiKeys : List<ApiKeyItem> { }

    public class ApiKeyItem
    {
        public string ApiKey { get; set; }
        public AllowAccess[] AllowAccess { get; set; }
    }

    public class AllowAccess
    {
        public string[] Resources { get; set; }
        public string[] Methods { get; set; }
    }
}