using System;

namespace RESTable.Auth;

public class AllCorsOriginsAllowed : IAllowedCorsOriginsFilter
{
    public bool IsAllowed(Uri uri)
    {
        return true;
    }
}