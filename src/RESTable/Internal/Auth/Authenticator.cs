using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RESTable.Meta;
using RESTable.Requests;
using RESTable.Results;

namespace RESTable.Internal.Auth
{
    /// <summary>
    /// The authenticator handles authorization and authentication of clients
    /// </summary>
    public class Authenticator
    {
        internal IDictionary<string, AccessRights> ApiKeys { get; private set; }
        internal HashSet<Uri> AllowedOrigins { get; private set; }

        private const string AuthHeaderMask = "*******";

        private RESTableConfiguration Configuration { get; }
        private RootAccess RootAccess { get; }

        public Authenticator(RESTableConfiguration configuration, RootAccess rootAccess)
        {
            Configuration = configuration;
            RootAccess = rootAccess;
        }

        /// <summary>
        /// Returns true if and only if this client is considered authenticated. This is a necessary precondition for 
        /// being included in a context. If false, a NotAuthorized result object is returned in the out parameter, that 
        /// can be returned to the client.
        /// </summary>
        /// <param name="context">The context to authenticate</param>
        /// <param name="uri">The URI of the request</param>
        /// <param name="headers">The headers of the request</param>
        /// <param name="error">The error result, if not authenticated</param>
        /// <returns></returns>
        public bool TryAuthenticate(RESTableContext context, ref string uri, out Unauthorized error, Headers headers = null)
        {
            context.Client.AccessRights = Configuration.RequireApiKey switch
            {
                true => GetAccessRights(ref uri, headers),
                false => RootAccess
            };
            if (context.Client.AccessRights == null)
            {
                error = new Unauthorized();
                error.SetContext(context);
                if (headers?.Metadata == "full")
                    error.Headers.Metadata = error.Metadata;
                return false;
            }
            error = null;
            return true;
        }

        private AccessRights GetAccessRights(ref string uri, IHeaders headers)
        {
            string authorizationHeader;
            if (uri != null && Regex.Match(uri, RegEx.UriKey) is Match {Success: true} keyMatch)
            {
                var keyGroup = keyMatch.Groups["key"];
                uri = uri.Remove(keyGroup.Index, keyGroup.Length);
                authorizationHeader = $"apikey {keyGroup.Value.Substring(1, keyGroup.Length - 2).UriDecode()}";
            }
            else if (headers.Authorization is string header && !string.IsNullOrWhiteSpace(header))
                authorizationHeader = header;
            else return null;
            headers.Authorization = AuthHeaderMask;
            var (method, key) = authorizationHeader.TSplit(' ');
            if (key == null) return null;
            switch (method)
            {
                case var apikey when apikey.EqualsNoCase("apikey"): break;
                case var basic when basic.EqualsNoCase("basic"):
                    key = Encoding.UTF8.GetString(Convert.FromBase64String(key)).Split(":").ElementAtOrDefault(1);
                    if (key == null) return null;
                    break;
                default: return null;
            }
            return ApiKeys.TryGetValue(key.SHA256(), out var _rights) ? _rights : null;
        }

        internal async Task RunResourceAuthentication<T>(IRequest<T> request, IEntityResource<T> resource) where T : class
        {
            if (request.Context.Client.ResourceAuthMappings.ContainsKey(resource))
                return;
            var authResults = await resource.AuthenticateAsync(request).ConfigureAwait(false);
            if (authResults.Success)
                request.Context.Client.ResourceAuthMappings[resource] = default;
            else throw new FailedResourceAuthentication(authResults.FailedReason);
        }
    }
}