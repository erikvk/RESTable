using System;
using System.Linq;
using System.Text;
using RESTar.Auth;
using RESTar.Requests;
using RESTar.Results.Fail.Forbidden;
using Starcounter;
using static RESTar.RESTarConfig;

namespace RESTar.Internal
{
    internal static class Authenticator
    {
        internal const string CurrentUser = "SELECT t.Token.User FROM Simplified.Ring5.SystemUserSession t " +
                                            "WHERE t.SessionIdString =? AND t.Token.User IS NOT NULL";

        internal static readonly string AppToken = Guid.NewGuid().ToString();

        private static object GetCurrentSystemUser()
        {
            if (Session.Current == null) return null;
            return Db.SQL(CurrentUser, Session.Current.SessionId).FirstOrDefault();
        }

        internal static void CheckUser()
        {
            if (GetCurrentSystemUser() == null)
                throw new UserNotSignedIn();
        }

        internal static void Authenticate<T>(this ViewRequest<T> request) where T : class
        {
            var user = GetCurrentSystemUser() ?? throw new UserNotSignedIn();
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
                throw new FailedResourceAuthentication(authResults.Reason);
        }

        internal static void Authenticate(this Arguments arguments)
        {
            arguments.AuthToken = GetAuthToken(arguments);
            if (arguments.AuthToken == null)
                arguments.Error = new NotAuthorized();
        }

        private static string GetAuthToken(Arguments arguments)
        {
            if (!RequireApiKey)
                return AssignAuthtoken(AccessRights.Root);
            AccessRights accessRights;
            if (arguments.TcpConnection.IsInternal)
            {
                var authToken = arguments.Headers.SafeGet("RESTar-AuthToken");
                if (authToken == null || !AuthTokens.TryGetValue(authToken, out accessRights))
                    return null;
                return authToken;
            }
            var authorizationHeader = arguments.Headers.SafeGet("Authorization");
            if (string.IsNullOrWhiteSpace(authorizationHeader)) return null;
            var (method, key) = authorizationHeader.TSplit(' ');
            if (key == null) return null;
            switch (method.ToLower())
            {
                case "apikey": break;
                case "basic":
                    key = Encoding.UTF8.GetString(Convert.FromBase64String(key)).Split(":").ElementAtOrDefault(1);
                    if (key == null) return null;
                    break;
                default: return null;
            }
            if (!ApiKeys.TryGetValue(key.SHA256(), out accessRights))
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

        internal static bool MethodCheck(Methods requestedMethod, IEntityResource resource, string authToken)
        {
            if (!resource.AvailableMethods.Contains(requestedMethod)) return false;
            var accessRights = AuthTokens[authToken];
            var rights = accessRights?[resource];
            return rights?.Contains(requestedMethod) == true;
        }
    }
}