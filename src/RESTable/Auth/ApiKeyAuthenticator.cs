using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using RESTable.Meta;
using RESTable.Requests;
using RESTable.WebSockets;

namespace RESTable.Auth;

public interface IApiKeyAuthenticator : IRequestAuthenticator;

/// <summary>
///     Handles authorization and authentication of clients using Api keys in either uris or headers, and
///     reads these api kets from an app configuration.
/// </summary>
public partial class ApiKeyAuthenticator : IApiKeyAuthenticator, IRequestAuthenticator
{
    private const string AuthHeaderMask = "ApiKey *******";

    public ApiKeyAuthenticator(IOptionsMonitor<ApiKeys> config, ResourceCollection resourceCollection, WebSocketManager webSocketManager)
    {
        ResourceCollection = resourceCollection;
        WebSocketManager = webSocketManager;
        ApiKeys = new Dictionary<string, AccessRights>();
        Config = config;
        Reload(config.CurrentValue);
        config.OnChange(Reload);
    }

    private IDictionary<string, AccessRights> ApiKeys { get; }
    private ResourceCollection ResourceCollection { get; }
    private WebSocketManager WebSocketManager { get; }
    private IOptionsMonitor<ApiKeys> Config { get; }

    /// <inheritdoc />
    public bool TryAuthenticate(ref string? uri, Headers? headers, out AccessRights accessRights)
    {
        accessRights = GetAccessRights(ref uri, headers);
        return accessRights is not NoAccess;
    }

    private AccessRights GetAccessRights(ref string? uri, IHeaders? headers)
    {
        string authorizationHeader;
        if (uri is not null && UriRegex().Match(uri) is { Success: true } keyMatch)
        {
            var keyGroup = keyMatch.Groups["key"];
            uri = uri.Remove(keyGroup.Index, keyGroup.Length);
            authorizationHeader = $"ApiKey {keyGroup.Value.Substring(1, keyGroup.Length - 2).UriDecode()}";
        }
        else if (headers?.Authorization is string header && !string.IsNullOrWhiteSpace(header))
        {
            authorizationHeader = header;
        }
        else
        {
            return new NoAccess();
        }
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
        return accessRights;
    }

    private void Reload(ApiKeys apiKeysConfiguration)
    {
        if (apiKeysConfiguration.Count <= 0)
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
                WebSocketManager.RevokeAllWithToken(key).Wait();
                accessRights.Clear();
            }
            ApiKeys.Remove(key);
        }
    }

    private static string ComputeHash(string input)
    {
        using var hasher = SHA256.Create();
        return Convert.ToBase64String(hasher.ComputeHash(Encoding.UTF8.GetBytes(input)));
    }

    private string ReadApiKey(ApiKeyItem apiKeyItem)
    {
        var apiKey = apiKeyItem.ApiKey;
        if (apiKey is null || !ApiKeyRegex().IsMatch(apiKey))
            throw new Exception("An API key contained invalid characters. Must be a non-empty string, not containing " +
                                "whitespace, and only containing ASCII characters 33 through 126");
        var keyHash = ComputeHash(apiKey);
        var assignments = AccessRights.CreateAssignments(apiKeyItem.AllowAccess ?? Array.Empty<AllowAccess>(), ResourceCollection);
        var accessRights = new AccessRights(this, keyHash, assignments);

        if (ApiKeys.TryGetValue(keyHash, out var existing))
        {
            existing.Clear();
            foreach (var (resource, value) in accessRights) existing[resource] = value;
        }
        else
        {
            ApiKeys[keyHash] = accessRights;
        }
        return keyHash;
    }

    [GeneratedRegex("^[^\\(]*(?<key>\\([^\\)]+\\))")]
    private static partial Regex UriRegex();

    [GeneratedRegex("^[!-~]+$")]
    private static partial Regex ApiKeyRegex();
}
