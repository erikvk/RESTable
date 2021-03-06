﻿using System;
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
    public class Authenticator
    {
        internal IDictionary<string, AccessRights> ApiKeys { get; private set; }
        internal HashSet<Uri> AllowedOrigins { get; private set; }

        internal void NewState()
        {
            ApiKeys = new Dictionary<string, AccessRights>();
            AllowedOrigins = new HashSet<Uri>();
        }

        internal const string AuthHeaderMask = "*******";

        internal async Task RunResourceAuthentication<T>(IRequest<T> request, IEntityResource<T> resource) where T : class
        {
            if (request.Context.Client.ResourceAuthMappings.ContainsKey(resource))
                return;
            var authResults = await resource.AuthenticateAsync(request).ConfigureAwait(false);
            if (authResults.Success)
                request.Context.Client.ResourceAuthMappings[resource] = default;
            else throw new FailedResourceAuthentication(authResults.FailedReason);
        }

        internal AccessRights GetAccessRights(string apiKeyHash)
        {
            return apiKeyHash != null && ApiKeys.TryGetValue(apiKeyHash, out var rights) ? rights : null;
        }

        internal AccessRights GetAccessRights(IHeaders headers)
        {
            string s = null;
            return GetAccessRights(ref s, headers);
        }

        internal AccessRights GetAccessRights(ref string uri, IHeaders headers)
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
    }
}