using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using RESTar.Requests;
using RESTar.Results;

namespace RESTar.Auth
{
    internal static class Authenticator
    {
        internal static IDictionary<string, AccessRights> ApiKeys { get; private set; }
        internal static IDictionary<AccessRights, byte> AccessRights { get; private set; }

        internal static void NewState()
        {
            ApiKeys = new Dictionary<string, AccessRights>();
            AccessRights = new ConcurrentDictionary<AccessRights, byte>();
        }

        internal static void RunResourceAuthentication<T>(this IRequest<T> request) where T : class
        {
            if (!request.Resource.RequiresAuthentication) return;
            var authResults = request.Resource.Authenticate(request);
            if (!authResults.Success)
                throw new FailedResourceAuthentication(authResults.Reason);
        }

        internal static AccessRights GetAccessRights(ref string uri, Headers headers)
        {
            string authorizationHeader = null;
            var keyMatch = Regex.Match(uri, RegEx.UriKey);
            if (keyMatch.Success)
            {
                var keypart = keyMatch.Groups["key"];
                uri = uri.Remove(keypart.Index, keypart.Length);
                authorizationHeader = $"apikey {HttpUtility.UrlDecode(keypart.Value.Substring(1, keypart.Length - 2))}";
                headers.Authorization = "*******";
            }
            else
            {
                if (headers.Authorization is string authorization)
                {
                    authorizationHeader = authorization;
                    headers.Authorization = "*******";
                }
            }
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
            return ApiKeys.TryGetValue(key.SHA256(), out var _rights) ? _rights : null;
        }
    }
}