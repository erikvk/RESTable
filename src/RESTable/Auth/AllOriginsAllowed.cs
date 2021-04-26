using System;

namespace RESTable.Auth
{
    public class AllOriginsAllowed : IAllowedOriginsFilter
    {
        public bool IsAllowed(Uri uri) => true;
    }
}