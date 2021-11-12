using System.Collections.Generic;

namespace RESTable
{
    public class AllowedCorsOrigins : List<string> { }

    public class ApiKeys : List<ApiKeyItem>
    {
        public const string ConfigSection = "RESTable.ApiKeys";
    }

    public class ApiKeyItem
    {
        public string? ApiKey { get; set; }
        public AllowAccess[]? AllowAccess { get; set; }
    }

    public class AllowAccess
    {
        public const string ConfigSection = "RESTable.AllowedCorsOrigins";

        public string[]? Resources { get; set; }
        public string[]? Methods { get; set; }
    }
}