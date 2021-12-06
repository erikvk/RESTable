using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using RESTable.Internal;
using RESTable.Results;

namespace RESTable.Requests;

/// <inheritdoc />
/// <summary>
///     Encodes a URI that is used in a request
/// </summary>
internal class URI : IUriComponents
{
    private URI()
    {
        Conditions = new List<IUriCondition>();
        MetaConditions = new List<IUriCondition>();
        ProtocolIdentifier = null!;
        ProtocolProvider = null!;
    }

    internal URI(string resourceSpecifier, string? viewName) : this()
    {
        ResourceSpecifier = resourceSpecifier;
        ViewName = viewName;
    }

    internal Exception? Error { get; private set; }
    internal bool HasError => Error is not null;
    public string ProtocolIdentifier { get; private set; }

    /// <inheritdoc />
    public string? ResourceSpecifier { get; private set; }

    /// <inheritdoc />
    public string? ViewName { get; private set; }

    /// <inheritdoc />
    public IReadOnlyCollection<IUriCondition> Conditions { get; private set; }

    /// <inheritdoc />
    public IReadOnlyCollection<IUriCondition> MetaConditions { get; private set; }

    public IMacro? Macro { get; private set; }

    public IProtocolProvider ProtocolProvider { get; set; }

    internal static URI ParseInternal
    (
        string uriString,
        bool percentCharsEscaped,
        RESTableContext context,
        out CachedProtocolProvider cachedProtocolProvider
    )
    {
        var uri = new URI();
        if (percentCharsEscaped) uriString = uriString.Replace("%25", "%");
        var groups = Regex.Match(uriString, RegEx.Protocol).Groups;
        var protocolString = groups["proto"].Value;
        if (protocolString.StartsWith("-"))
            protocolString = protocolString.Substring(1);
        var tail = groups["tail"].Value;
        uri.ProtocolIdentifier = protocolString.ToLowerInvariant();
        var protocolProviderManager = context.GetRequiredService<ProtocolProviderManager>();
        if (!protocolProviderManager.CachedProtocolProviders.TryGetValue(protocolString, out var _cachedProtocolProvider))
        {
            uri.Error = new UnknownProtocol(protocolString);
            cachedProtocolProvider = protocolProviderManager.DefaultProtocolProvider;
            return uri;
        }
        cachedProtocolProvider = _cachedProtocolProvider;
        uri.ProtocolProvider = cachedProtocolProvider.ProtocolProvider;
        try
        {
            uri.Populate(cachedProtocolProvider.ProtocolProvider.GetUriComponents(tail, context));
        }
        catch (Exception e)
        {
            uri.Error = e;
        }
        return uri;
    }

    private void Populate(IUriComponents components)
    {
        ResourceSpecifier = components.ResourceSpecifier;
        ViewName = components.ViewName;
        Conditions = components.Conditions;
        MetaConditions = components.MetaConditions;
        Macro = components.Macro;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return this.ToUriString();
    }
}