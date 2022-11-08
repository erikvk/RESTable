using System;

namespace RESTable.Auth;

public interface IAllowedCorsOriginsFilter
{
    bool IsAllowed(Uri uri);
}
