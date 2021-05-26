using RESTable.Requests;
using RESTable.Results;

namespace RESTable.Auth
{
    public interface IRequestAuthenticator
    {
        /// <summary>
        /// Returns true if and only if the client of this context is considered authenticated. If false,
        /// a Unauthorized error result object describes the case, that can be returned to the client.
        /// </summary>
        /// <param name="uri">The URI of the request</param>
        /// <param name="headers">The headers of the request</param>
        /// <param name="accessRights">The access rights associated with the authenticated client</param>
        /// <param name="error">The error result, if not authenticated</param>
        /// <returns></returns>
        bool TryAuthenticate(ref string uri, Headers headers, out AccessRights accessRights, out Unauthorized error);
    }
}