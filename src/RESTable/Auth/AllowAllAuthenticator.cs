using RESTable.Requests;
using RESTable.Results;

namespace RESTable.Auth
{
    /// <summary>
    /// Assigns root access to all requests. This is the default request authenticator.
    /// </summary>
    public class AllowAllAuthenticator : IRequestAuthenticator
    {
        private RootAccess RootAccess { get; }

        public AllowAllAuthenticator(RootAccess rootAccess)
        {
            RootAccess = rootAccess;
        }

        public bool TryAuthenticate(RESTableContext context, ref string uri, Headers headers, out Unauthorized error)
        {
            context.Client.AccessRights = RootAccess;
            error = null;
            return true;
        }
    }
}