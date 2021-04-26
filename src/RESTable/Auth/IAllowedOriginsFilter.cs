using System;

namespace RESTable.Auth
{
    public interface IAllowedOriginsFilter
    {
        bool IsAllowed(Uri uri);
    }
}