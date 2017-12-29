using System;
using System.Linq;
using System.Net;
using RESTar.Auth;
using RESTar.Requests;
using Starcounter;
using static RESTar.Internal.ErrorCodes;
using static RESTar.RESTarConfig;

namespace RESTar.Internal
{
    internal static class Authenticator
    {
        internal const string CurrentUser = "SELECT t.Token.User FROM Simplified.Ring5.SystemUserSession t " +
                                            "WHERE t.SessionIdString =? AND t.Token.User IS NOT NULL";

        internal static Forbidden NotAuthorizedException => new Forbidden(NotAuthorized,
            "Not authorized");

        internal static readonly string AppToken = Guid.NewGuid().ToString();

        private static object GetCurrentSystemUser()
        {
            if (Session.Current == null) return null;
            return Db.SQL(CurrentUser, Session.Current.SessionId).FirstOrDefault();
        }

        internal static void CheckUser()
        {
            if (GetCurrentSystemUser() == null)
                throw new Forbidden(NotSignedIn, "User is not signed in");
        }

        internal static void Authenticate<T>(this ViewRequest<T> request) where T : class
        {
            var user = GetCurrentSystemUser() ?? throw new Forbidden(NotSignedIn, "User is not signed in");
            var token = user.GetObjectID().SHA256();
            if (AuthTokens.ContainsKey(token))
                request.AuthToken = token;
            request.AuthToken = AssignAuthtoken(AccessRights.Root, token);
        }

        internal static void RunResourceAuthentication<T>(this IRequest<T> request) where T : class
        {
            if (!request.Resource.RequiresAuthentication) return;
            var authResults = request.Resource.Authenticate(request);
            if (!authResults.Success)
                throw new Forbidden(FailedResourceAuthentication, authResults.Reason);
        }

        internal static void Authenticate<T>(this RESTRequest<T> request, Arguments arguments) where T : class
        {
            if (!RequireApiKey)
            {
                request.AuthToken = AssignAuthtoken(AccessRights.Root);
                return;
            }
            AccessRights accessRights;
            if (!arguments.Origin.IsExternal)
            {
                var authToken = arguments.Headers.SafeGet("RESTar-AuthToken");
                if (string.IsNullOrWhiteSpace(authToken))
                    throw NotAuthorizedException;
                if (!AuthTokens.TryGetValue(authToken, out accessRights))
                    throw NotAuthorizedException;
                request.AuthToken = authToken;
                return;
            }
            var authorizationHeader = arguments.Headers.SafeGet("Authorization");
            if (string.IsNullOrWhiteSpace(authorizationHeader))
            {
                if (!arguments.UriMetaConditions.Any()) throw NotAuthorizedException;
                var keyMetaCondition = arguments.UriMetaConditions.FirstOrDefault(c => c.Key.EqualsNoCase("key"));
                if (keyMetaCondition.ValueLiteral == null) throw NotAuthorizedException;
                arguments.UriMetaConditions.Remove(keyMetaCondition);
                authorizationHeader = $"apikey {WebUtility.UrlDecode(keyMetaCondition.ValueLiteral)}";
            }
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

        internal static bool MethodCheck(Methods requestedMethod, IResource resource, string authToken)
        {
            if (!resource.AvailableMethods.Contains(requestedMethod)) return false;
            var accessRights = AuthTokens[authToken];
            var rights = accessRights?[resource];
            return rights?.Contains(requestedMethod) == true;
        }
    }
}