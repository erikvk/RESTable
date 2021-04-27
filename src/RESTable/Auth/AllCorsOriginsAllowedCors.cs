using System;

namespace RESTable.Auth
{
    public class AllCorsOriginsAllowedCors : IAllowedCorsOriginsFilter
    {
        public bool IsAllowed(Uri uri) => true;
    }
}