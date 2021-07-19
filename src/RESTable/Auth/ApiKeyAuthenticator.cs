using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using RESTable.Meta;
using RESTable.Requests;
using RESTable.WebSockets;

namespace RESTable.Auth
{
    public interface IApiKeyAuthenticator : IRequestAuthenticator, IDisposable { }

    /// <summary>
    /// Handles authorization and authentication of clients using Api keys in either uris or headers, and
    /// reads these api kets from an app configuration.
    /// </summary>
    public class ApiKeyAuthenticator : IApiKeyAuthenticator, IRequestAuthenticator, IDisposable
    {
        private const string AuthHeaderMask = "ApiKey *******";
        private const string ApiKeysConfigSection = "RESTable.ApiKeys";

        private IDictionary<string, AccessRights> ApiKeys { get; }
        private ResourceCollection ResourceCollection { get; }
        private WebSocketManager WebSocketManager { get; }
        private IConfiguration Configuration { get; }
        private IDisposable ReloadToken { get; }

        public ApiKeyAuthenticator(IConfiguration configuration, ResourceCollection resourceCollection, WebSocketManager webSocketManager)
        {
            ResourceCollection = resourceCollection;
            WebSocketManager = webSocketManager;
            ApiKeys = new Dictionary<string, AccessRights>();
            Configuration = configuration;
            Reload();
            ReloadToken = ChangeToken.OnChange(Configuration.GetReloadToken, Reload);
        }

        /// <inheritdoc />
        public bool TryAuthenticate(ref string? uri, Headers? headers, out AccessRights accessRights)
        {
            accessRights = GetAccessRights(ref uri, headers);
            return accessRights is not NoAccess;
        }

        private AccessRights GetAccessRights(ref string? uri, IHeaders? headers)
        {
            string authorizationHeader;
            if (uri is not null && Regex.Match(uri, RegEx.UriKey) is {Success: true} keyMatch)
            {
                var keyGroup = keyMatch.Groups["key"];
                uri = uri.Remove(keyGroup.Index, keyGroup.Length);
                authorizationHeader = $"ApiKey {keyGroup.Value.Substring(1, keyGroup.Length - 2).UriDecode()}";
            }
            else if (headers?.Authorization is string header && !string.IsNullOrWhiteSpace(header))
                authorizationHeader = header;
            else return new NoAccess();
            if (headers is not null)
                headers.Authorization = AuthHeaderMask;
            var (method, key) = authorizationHeader.TupleSplit(' ');
            if (key is null)
                return new NoAccess();
            switch (method)
            {
                case var apikey when apikey.EqualsNoCase("apikey"): break;
                case var basic when basic.EqualsNoCase("basic"):
                {
                    var keyString = Convert.FromBase64String(key);
                    key = Encoding.UTF8.GetString(keyString).Split(":").ElementAtOrDefault(1);
                    if (key is null) return new NoAccess();
                    break;
                }
                default: return new NoAccess();
            }
            var keyHash = ComputeHash(key);
            if (!ApiKeys.TryGetValue(keyHash, out var accessRights))
                return new NoAccess();
            return accessRights!;
        }

        private void Reload()
        {
            var apiKeysConfiguration = Configuration.GetSection(ApiKeysConfigSection).Get<ApiKeys>();
            if (apiKeysConfiguration?.Count is not > 0)
                throw new InvalidOperationException($"When using {nameof(ApiKeyAuthenticator)}, the application configuration file is used " +
                                                    "to read API keys. The config file is missing an 'ApiKeys' array with at least one " +
                                                    "'ApiKey' item.");
            ReadApiKeys(apiKeysConfiguration);
        }

        private void ReadApiKeys(ApiKeys apiKeysConfiguration)
        {
            var currentKeys = apiKeysConfiguration
                .Select(ReadApiKey)
                .ToList();
            foreach (var key in ApiKeys.Keys.Except(currentKeys).ToList())
            {
                if (ApiKeys.TryGetValue(key, out var accessRights))
                {
                    WebSocketManager.RevokeAllWithKey(key).Wait();
                    accessRights!.Clear();
                }
                ApiKeys.Remove(key);
            }
        }

        private static string ComputeHash(string input)
        {
            using var hasher = System.Security.Cryptography.SHA256.Create();
            return Convert.ToBase64String(hasher.ComputeHash(Encoding.UTF8.GetBytes(input)));
        }

        private string ReadApiKey(ApiKeyItem apiKeyItem)
        {
            var apiKey = apiKeyItem.ApiKey;
            if (apiKey is null || Regex.IsMatch(apiKey, @"[\(\)]") || !Regex.IsMatch(apiKey, RegEx.ApiKey))
                throw new Exception("An API key contained invalid characters. Must be a non-empty string, not containing " +
                                    "whitespace or parentheses, and only containing ASCII characters 33 through 126");
            var keyHash = ComputeHash(apiKey);
            var assignments = AccessRights.CreateAssignments(apiKeyItem.AllowAccess ?? Array.Empty<AllowAccess>(), ResourceCollection);
            var accessRights = new AccessRights(keyHash, assignments);

            if (ApiKeys.TryGetValue(keyHash, out var existing))
            {
                existing!.Clear();
                foreach (var (resource, value) in accessRights)
                {
                    existing[resource] = value;
                }
            }
            else ApiKeys[keyHash] = accessRights;
            return keyHash;
        }

        public void Dispose() => ReloadToken.Dispose();
    }
}