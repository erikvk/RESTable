using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using RESTable.Internal;
using RESTable.Meta;
using RESTable.Meta.Internal;
using RESTable.Requests;
using RESTable.Results;
using RESTable.WebSockets;
using static RESTable.Method;

namespace RESTable.Auth
{
    /// <summary>
    /// Handles authorization and authentication of clients using Api keys in either uris or headers
    /// </summary>
    public class ApiKeyAuthenticator : IRequestAuthenticator
    {
        private const string AuthHeaderMask = "*******";

        private IDictionary<string, AccessRights> ApiKeys { get; }
        private ResourceCollection ResourceCollection { get; }
        private WebSocketManager WebSocketManager { get; }
        private IOptionsSnapshot<ApiKeys> ApiKeysConfiguration { get; }

        public ApiKeyAuthenticator(IOptionsSnapshot<ApiKeys> apiKeysConfiguration, ResourceCollection resourceCollection, WebSocketManager webSocketManager)
        {
            ApiKeysConfiguration = apiKeysConfiguration;
            ResourceCollection = resourceCollection;
            WebSocketManager = webSocketManager;
            ApiKeys = new Dictionary<string, AccessRights>();
            ReadApiKeys();
        }

        /// <inheritdoc />
        public bool TryAuthenticate(RESTableContext context, ref string uri, Headers headers, out Unauthorized error)
        {
            var accessRights = GetAccessRights(ref uri, headers);
            if (accessRights == null)
            {
                error = new Unauthorized();
                error.SetContext(context);
                if (headers?.Metadata == "full")
                    error.Headers.Metadata = error.Metadata;
                return false;
            }
            context.Client.AccessRights = accessRights;
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
            var keyHash = key.SHA256();
            return ApiKeys.TryGetValue(keyHash, out var _rights) ? _rights : null;
        }

        private void ReadApiKeys()
        {
            var currentKeys = ApiKeysConfiguration.Value
                .Select(ReadApiKey)
                .ToList();
            foreach (var key in ApiKeys.Keys.Except(currentKeys).ToList())
            {
                if (ApiKeys.TryGetValue(key, out var accessRights))
                {
                    WebSocketManager.RevokeAllWithKey(key).Wait();
                    accessRights.Clear();
                }
                ApiKeys.Remove(key);
            }
        }

        private IEnumerable<Method> GetMethods(IEnumerable<string> methodsArray)
        {
            foreach (var method in methodsArray)
            {
                var trimmed = method.Trim();
                if (trimmed == "*")
                {
                    foreach (var item in EnumMember<Method>.Values)
                        yield return item;
                    yield break;
                }
                yield return (Method) Enum.Parse(typeof(Method), method.ToUpperInvariant());
            }
        }

        private string ReadApiKey(ApiKeyItem key)
        {
            var apiKey = key.ApiKey;
            if (apiKey == null || Regex.IsMatch(apiKey, @"[\(\)]") || !Regex.IsMatch(apiKey, RegEx.ApiKey))
                throw new Exception("An API key contained invalid characters. Must be a non-empty string, not containing " +
                                    "whitespace or parentheses, and only containing ASCII characters 33 through 126");
            var keyHash = apiKey.SHA256();
            var accessRightsEnumeration = key.AllowAccess.Select(item => new AccessRight
            (
                resources: item.Resources
                    .Select(resource => ResourceCollection.SafeFindResources(resource))
                    .SelectMany(iresources => iresources.Union(iresources.Cast<IResourceInternal>()
                        .Where(r => r.InnerResources != null)
                        .SelectMany(r => r.InnerResources)))
                    .OrderBy(r => r.Name)
                    .ToList(),
                allowedMethods: GetMethods(item.Methods)
                    .Distinct()
                    .OrderBy(i => i, MethodComparer.Instance)
                    .ToArray()
            ));
            var accessRights = AccessRights.ToAccessRights(accessRightsEnumeration, keyHash);
            foreach (var resource in ResourceCollection.Where(r => r.GETAvailableToAll))
            {
                if (accessRights.TryGetValue(resource, out var methods))
                    accessRights[resource] = methods.Union(new[] {GET, REPORT, HEAD})
                        .OrderBy(i => i, MethodComparer.Instance)
                        .ToArray();
                else accessRights[resource] = new[] {GET, REPORT, HEAD};
            }
            if (ApiKeys.TryGetValue(keyHash, out var existing))
            {
                existing.Clear();
                foreach (var (resource, value) in accessRights) existing[resource] = value;
            }
            else ApiKeys[keyHash] = accessRights;
            return keyHash;
        }
    }
}