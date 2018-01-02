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

        internal static Forbidden NotAuthorized => new Forbidden(ErrorCodes.NotAuthorized, "Not authorized");

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

        internal static string GetAuthToken(this Arguments arguments)
        {
            if (!RequireApiKey)
                return AssignAuthtoken(AccessRights.Root);
            AccessRights accessRights;
            if (arguments.Origin.IsInternal)
            {
                var authToken = arguments.Headers.SafeGet("RESTar-AuthToken");
                if (authToken == null || !AuthTokens.TryGetValue(authToken, out accessRights))
                    return null;
                return authToken;
            }
            var authorizationHeader = arguments.Headers.SafeGet("Authorization");
            if (string.IsNullOrWhiteSpace(authorizationHeader))
            {
                if (!arguments.Uri.MetaConditions.Any()) return null;
                var keyMetaCondition = arguments.Uri.MetaConditions.FirstOrDefault(c => c.Key.EqualsNoCase("key"));
                if (keyMetaCondition.ValueLiteral == null) return null;
                arguments.Uri.MetaConditions.Remove(keyMetaCondition);
                authorizationHeader = $"apikey {WebUtility.UrlDecode(keyMetaCondition.ValueLiteral)}";
            }
            var apikey_key = authorizationHeader.Split(' ');
            if (apikey_key[0].ToLower() != "apikey" || apikey_key.Length != 2)
                return null;
            var apiKey = apikey_key[1].SHA256();
            if (!ApiKeys.TryGetValue(apiKey, out accessRights))
                return null;
            return AssignAuthtoken(accessRights);
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