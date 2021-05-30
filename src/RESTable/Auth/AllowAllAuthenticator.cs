using RESTable.Requests;

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

        public bool TryAuthenticate(ref string uri, Headers headers, out AccessRights accessRights)
        {
            accessRights = RootAccess;
            return true;
        }
    }
}