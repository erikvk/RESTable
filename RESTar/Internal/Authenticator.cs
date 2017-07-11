using System;
using System.Linq;
using RESTar.Auth;
using RESTar.Requests;
using Starcounter;
using static RESTar.Internal.ErrorCodes;
using static RESTar.RESTarConfig;
using static Simplified.Ring3.SystemUser;

namespace RESTar.Internal
{
    internal static class Authenticator
    {
        internal static ForbiddenException NotAuthorizedException => new ForbiddenException(NotAuthorized,
            "Not authorized");

        internal static readonly string AppToken = Guid.NewGuid().ToString();

        internal static void CheckUser()
        {
            if (GetCurrentSystemUser() == null)
                throw new ForbiddenException(NotSignedIn, "User is not signed in");
        }

        internal static void Authenticate<T>(this ViewRequest<T> request) where T : class
        {
            var user = GetCurrentSystemUser() ?? throw new ForbiddenException(NotSignedIn, "User is not signed in");
            var token = user.GetObjectID().SHA256();
            if (AuthTokens.ContainsKey(token))
                request.AuthToken = token;
            request.AuthToken = AssignAuthtoken(AccessRights.Root, token);
        }

        internal static void Authenticate<T>(this RESTRequest<T> request) where T : class
        {
            if (!RequireApiKey)
                request.AuthToken = AssignAuthtoken(AccessRights.Root);
            AccessRights accessRights;
            if (!request.ScRequest.IsExternal)
            {
                var authToken = request.ScRequest.Headers["RESTar-AuthToken"];
                if (string.IsNullOrWhiteSpace(authToken))
                    throw NotAuthorizedException;
                if (!AuthTokens.TryGetValue(authToken, out accessRights))
                    throw NotAuthorizedException;
                request.AuthToken = authToken;
                return;
            }
            var authorizationHeader = request.ScRequest.Headers["Authorization"];
            if (string.IsNullOrWhiteSpace(authorizationHeader))
                throw NotAuthorizedException;
            var apikey_key = authorizationHeader.Split(' ');
            if (apikey_key[0].ToLower() != "apikey" || apikey_key.Length != 2)
                throw NotAuthorizedException;
            var apiKey = apikey_key[1].SHA256();
            if (!ApiKeys.TryGetValue(apiKey, out accessRights))
                throw NotAuthorizedException;
            request.AuthToken = AssignAuthtoken(accessRights);
        }

        internal static void Authenticate<T>(this Request<T> request) where T : class
        {
            request.AuthToken = AppToken;
        }

        private static string AssignAuthtoken(AccessRights rights, string token = null)
        {
            token = token ?? Guid.NewGuid().ToString();
            AuthTokens[token] = rights;
            return token;
        }

        internal static bool MethodCheck(RESTarMethods requestedMethod, IResource resource, string authToken)
        {
            if (!resource.AvailableMethods.Contains(requestedMethod)) return false;
            var accessRights = AuthTokens[authToken];
            var rights = accessRights?[resource];
            return rights?.Contains(requestedMethod) == true;
        }
    }
}