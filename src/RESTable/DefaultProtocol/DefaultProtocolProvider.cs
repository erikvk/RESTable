using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using RESTable.ContentTypeProviders;
using RESTable.DefaultProtocol.Serialized;
using RESTable.Internal;
using RESTable.Meta;
using RESTable.Requests;
using RESTable.Results;
using RESTable.WebSockets;

namespace RESTable.DefaultProtocol;

/// <inheritdoc />
/// <summary>
///     Contains the logic for the default RESTable protocol. This protocol is used if no
///     protocol indicator is included in the request URI.
/// </summary>
internal sealed partial class DefaultProtocolProvider : IProtocolProvider
{
    public DefaultProtocolProvider(IJsonProvider jsonProvider, ResourceCollection resourceCollection)
    {
        ResourceCollection = resourceCollection;
        JsonProvider = jsonProvider;
    }

    private IJsonProvider JsonProvider { get; }
    private ResourceCollection ResourceCollection { get; }

    /// <inheritdoc />
    public string ProtocolName => "RESTable";

    /// <inheritdoc />
    public string ProtocolIdentifier => "restable";

    /// <inheritdoc />
    public IUriComponents GetUriComponents(string uriString, RESTableContext context)
    {
        var uri = new DefaultProtocolUriComponents(this);

        var secondSlash = AllIndexesOf(uriString, '/').Skip(1).FirstOrDefault(short.MaxValue);
        var sqIndex = uriString.IndexOf("/?", StringComparison.Ordinal);
        if (sqIndex is not -1 && sqIndex == secondSlash)
            uriString = string.Join('/', uriString[..sqIndex], uriString[(sqIndex + 2)..]);
        else
        {
            var firstQ = uriString.IndexOf('?', StringComparison.Ordinal);
            if (firstQ is not -1 && firstQ < secondSlash)
            {
                uriString = string.Join("/", uriString[..firstQ], uriString[(firstQ + 1)..]);
            }
        }

        var match = RequestUriRegex().Match(uriString);
        if (!match.Success) throw new InvalidSyntax(ErrorCodes.InvalidUriSyntax, "Check URI syntax");
        var resourceOrMacro = match.Groups["res"].Value.TrimStart('/');
        var view = match.Groups["view"].Value.TrimStart('-');
        var conditions = match.Groups["cond"].Value.TrimStart('/');
        var metaConditions = match.Groups["meta"].Value.TrimStart('/');

        switch (conditions)
        {
            case "":
            case "_": break;
            default:
            {
                foreach (var uriCondition in ParseUriConditions(conditions, true))
                    uri.Conditions.Add(uriCondition);
                break;
            }
        }

        switch (metaConditions)
        {
            case "":
            case "_": break;
            default:
            {
                foreach (var uriCondition in ParseUriConditions(metaConditions, true))
                    uri.MetaConditions.Add(uriCondition);
                break;
            }
        }

        if (view.Length != 0)
            uri.ViewName = view;

        switch (resourceOrMacro)
        {
            case "":
            case "_":
                uri.ResourceSpecifier = context.WebSocket?.Status == WebSocketStatus.Waiting
                    ? ResourceCollection.GetResourceSpecifier<Shell>()
                    : ResourceCollection.GetResourceSpecifier<AvailableResource>();
                break;
            case var resource when resourceOrMacro[0] != '$':
                uri.ResourceSpecifier = resource;
                break;
        }

        return uri;
    }

    public ExternalContentTypeProviderSettings ExternalContentTypeProviderSettings => ExternalContentTypeProviderSettings.AllowAll;

    public IEnumerable<IContentTypeProvider>? GetCustomContentTypeProviders()
    {
        return null;
    }

    public IContentTypeProvider GetDefaultInputContentTypeProvider(ICollection<IContentTypeProvider> registeredProviders)
    {
        var json = registeredProviders.FirstOrDefault(p => p.ContentType == ContentType.JSON);
        if (json is not null)
            return json;
        return registeredProviders.First();
    }

    public IContentTypeProvider GetDefaultOutputContentTypeProvider(ICollection<IContentTypeProvider> registeredProviders)
    {
        var json = registeredProviders.FirstOrDefault(p => p.ContentType == ContentType.JSON);
        if (json is not null)
            return json;
        return registeredProviders.First();
    }

    /// <inheritdoc />
    public string MakeRelativeUri(IUriComponents components)
    {
        return ToUriString(components);
    }

    public void SetResultHeaders(IResult result) { }

    /// <inheritdoc />
    public Task SerializeResult(ISerializedResult toSerialize, IContentTypeProvider contentTypeProvider, CancellationToken cancellationToken)
    {
        switch (toSerialize.Result)
        {
            case Options options: return SerializeOptions(options, toSerialize, contentTypeProvider, cancellationToken);
            case Head: return Task.CompletedTask;
            case Change change: return SerializeChange((dynamic) change, toSerialize, contentTypeProvider, cancellationToken);
            case Binary binary: return binary.BinaryResult.WriteToStream(toSerialize.Body, cancellationToken);
            case IEntities<object> entities: return SerializeEntities((dynamic) entities, toSerialize, contentTypeProvider, cancellationToken);
            case Report report: return SerializeReport(report, toSerialize, contentTypeProvider, cancellationToken);
            case Error error: return SerializeError(error, toSerialize, contentTypeProvider, cancellationToken);
            default: return Task.CompletedTask;
        }
    }


    public bool IsCompliant(IRequest request, out string? invalidReason)
    {
        invalidReason = null;
        return true;
    }

    public void OnInit() { }

    internal static List<IUriCondition> ParseUriConditions(string conditionsString, bool check = false)
    {
        return conditionsString
            .Split('&')
            .Select(s => ParseUriCondition(s, check))
            .ToList();
    }

    internal static IUriCondition ParseUriCondition(string conditionString, bool check = false)
    {
        if (check)
        {
            if (string.IsNullOrEmpty(conditionString))
                throw new InvalidSyntax(ErrorCodes.InvalidConditionSyntax, "Invalid condition syntax");
            conditionString = conditionString.ReplaceFirst("%3E=", ">=", out var replaced);
            if (!replaced) conditionString = conditionString.ReplaceFirst("%3C=", "<=", out replaced);
            if (!replaced) conditionString = conditionString.ReplaceFirst("%3E", ">", out replaced);
            if (!replaced) conditionString = conditionString.ReplaceFirst("%3C", "<", out replaced);
        }
        var match = UriConditionRegex().Match(conditionString);
        if (!match.Success) throw new InvalidSyntax(ErrorCodes.InvalidConditionSyntax, $"Invalid condition syntax at '{conditionString}'");
        var (key, opString, valueLiteral) = (match.Groups["key"].Value, match.Groups["op"].Value, match.Groups["val"].Value);
        if (!Operator.TryParse(opString, out var @operator)) throw new InvalidOperator(conditionString);
        return new UriCondition
        (
            key.UriDecode(),
            @operator.OpCode,
            valueLiteral.UriDecode(),
            TypeCode.String
        );
    }

    private static string ToUriString(IUriComponents components)
    {
        var view = components.ViewName is not null ? $"-{components.ViewName}" : null;
        var resource = components.Macro is null ? $"/{components.ResourceSpecifier}{view}" : $"/${components.Macro.Name}";
        var str = new StringBuilder(resource);
        if (components.Conditions.Count > 0)
        {
            str.Append($"/{ToUriString(components.Conditions)}");
            if (components.MetaConditions.Count > 0)
                str.Append($"/{UnescapeMetaconditions(ToUriString(components.MetaConditions))}");
        }
        else if (components.MetaConditions.Count > 0)
        {
            str.Append($"/_/{UnescapeMetaconditions(ToUriString(components.MetaConditions))}");
        }
        return str.ToString();
    }

    private static string UnescapeMetaconditions(string metaconditionsString)
    {
        return metaconditionsString
            .Replace("%2C", ",")
            .Replace("%3E", ">")
            .Replace("%24", "$");
    }

    internal static string ToUriString(IEnumerable<IUriCondition>? conditions)
    {
        if (conditions is null) return "_";
        var uriString = string.Join("&", conditions.Select(ToUriString));
        if (uriString.Length == 0) return "_";
        return uriString;
    }

    private static string ToUriString(IUriCondition? condition)
    {
        if (condition is null) return "";
        var op = ((Operator) condition.Operator).Common;
        var value = ToUriValueString(condition);
        return $"{condition.Key.UriEncode()}{op}{value}";
    }

    private static string ToUriValueString(IUriCondition condition)
    {
        var valueLiteral = condition.ValueLiteral;
        switch (condition.ValueTypeCode)
        {
            case TypeCode.Empty: return "null";
            case TypeCode.Char:
            case TypeCode.String:
                var encoded = valueLiteral.UriEncode();
                switch (encoded)
                {
                    case null: return "null";
                    case "false":
                    case "False":
                    case "FALSE":
                    case "true":
                    case "True":
                    case "TRUE":
                    case var _ when encoded.All(char.IsDigit): return $"'{encoded}'";
                    default: return encoded;
                }
            default: return valueLiteral;
        }
    }

    private static async Task SerializeOptions(Options options, ISerializedResult toSerialize, IContentTypeProvider contentTypeProvider, CancellationToken cancellationToken)
    {
        if (contentTypeProvider is not IJsonProvider jsonProvider)
            return;
        if (options.Resource is not IResource resource)
            return;
        var optionsBody = new OptionsBody(resource.Name, resource.ResourceKind, resource.AvailableMethods);
        var serializedOptions = new SerializedOptions(optionsBody);
        await jsonProvider.SerializeAsync(toSerialize.Body, serializedOptions, cancellationToken: cancellationToken).ConfigureAwait(false);
        toSerialize.EntityCount = serializedOptions.DataCount;
    }

    private static async Task SerializeError(Error error, ISerializedResult toSerialize, IContentTypeProvider contentTypeProvider, CancellationToken cancellationToken)
    {
        if (contentTypeProvider is not IJsonProvider jsonProvider)
            return;

        string? uri = null;
        if (error.Request.UriComponents is IUriComponents uriComponents) uri = ToUriString(uriComponents);
        if (error is InvalidInputEntity invalidInputEntity)
        {
            var serializedInvalidInputEntity = new SerializedInvalidEntity(invalidInputEntity, uri);
            await jsonProvider.SerializeAsync(toSerialize.Body, serializedInvalidInputEntity, cancellationToken: cancellationToken).ConfigureAwait(false);
            toSerialize.EntityCount = 1;
        }
        else
        {
            var serializedError = new SerializedError(error, uri);
            await jsonProvider.SerializeAsync(toSerialize.Body, serializedError, cancellationToken: cancellationToken).ConfigureAwait(false);
            toSerialize.EntityCount = 0;
        }
    }

    private static async Task SerializeEntities<T>
    (
        IEntities<T> entities,
        ISerializedResult toSerialize,
        IContentTypeProvider contentTypeProvider,
        CancellationToken cancellationToken
    ) where T : class
    {
        if (contentTypeProvider is not IJsonProvider jsonProvider)
        {
            var count = await contentTypeProvider.SerializeAsyncEnumerable(toSerialize.Body, entities, cancellationToken).ConfigureAwait(false);
            toSerialize.EntityCount = count;
            return;
        }

        if (RequestsRawJson(entities, out var single))
        {
            if (single)
            {
                var singleItem = await entities.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
                toSerialize.EntityCount = 1;
                await jsonProvider.SerializeAsync(toSerialize.Body, singleItem, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await SerializeRaw(entities, toSerialize, jsonProvider, cancellationToken).ConfigureAwait(false);
            }
            return;
        }

        var serializedContent = new SerializedEntitiesAsyncEnumerable<T>(entities, toSerialize);
        await jsonProvider.SerializeAsync(toSerialize.Body, serializedContent, cancellationToken: cancellationToken).ConfigureAwait(false);
        toSerialize.EntityCount = serializedContent.DataCount;
    }

    private static Task SerializeChange<T>(Change<T> change, ISerializedResult toSerialize, IContentTypeProvider contentTypeProvider, CancellationToken cancellationToken)
        where T : class
    {
        toSerialize.EntityCount = change.Count;
        if (contentTypeProvider is not IJsonProvider jsonProvider)
            return Task.CompletedTask;

        if (RequestsRawJson(change, out var single))
        {
            if (single)
            {
                var singleItem = change.Entities.FirstOrDefault();
                toSerialize.EntityCount = singleItem is null ? 0 : 1;
                return jsonProvider.SerializeAsync(toSerialize.Body, singleItem, cancellationToken: cancellationToken);
            }
            toSerialize.EntityCount = change.Count;
            return jsonProvider.SerializeAsync(toSerialize.Body, change.Entities, cancellationToken: cancellationToken);
        }

        var serializedChange = new SerializedChange<T>(change);
        return jsonProvider.SerializeAsync(toSerialize.Body, serializedChange, cancellationToken: cancellationToken);
    }

    private static Task SerializeReport(Report report, ISerializedResult toSerialize, IContentTypeProvider contentTypeProvider, CancellationToken cancellationToken)
    {
        toSerialize.EntityCount = 1;
        if (contentTypeProvider is not IJsonProvider jsonProvider)
            return Task.CompletedTask;

        if (RequestsRawJson(report, out _))
        {
            toSerialize.EntityCount = 1;
            return jsonProvider.SerializeAsync(toSerialize.Body, report.Count, cancellationToken: cancellationToken);
        }

        var serializedReport = new SerializedReport(report);
        return jsonProvider.SerializeAsync(toSerialize.Body, serializedReport, cancellationToken: cancellationToken);
    }

    private static bool RequestsRawJson(IResult result, out bool single)
    {
        var data = result.Request.Headers.Accept?.FirstOrDefault().Data;
        if (data is null)
        {
            single = false;
            return false;
        }
        single = data.TryGetValue("single", out var singleValue) && singleValue == "true";
        return data.TryGetValue("raw", out var rawValue) && rawValue == "true";
    }

    private static async Task SerializeRaw<T>
    (
        IAsyncEnumerable<T> entities,
        ISerializedResult toSerialize,
        IJsonProvider jsonProvider,
        CancellationToken cancellationToken
    )
        where T : class
    {
        var counter = 0L;

        async IAsyncEnumerable<T> enumerateAndCount(IAsyncEnumerable<T> _entities)
        {
            await foreach (var entity in _entities.ConfigureAwait(false))
            {
                counter += 1;
                yield return entity;
            }
        }

        await jsonProvider.SerializeAsync(toSerialize.Body, enumerateAndCount(entities), cancellationToken: cancellationToken).ConfigureAwait(false);
        toSerialize.EntityCount = counter;
    }

    public static IEnumerable<int> AllIndexesOf(string str, char searchChar)
    {
        var minIndex = str.IndexOf(searchChar);
        while (minIndex != -1)
        {
            yield return minIndex;
            minIndex = str.IndexOf(searchChar, minIndex + 1);
        }
    }

    [GeneratedRegex("""^(?<ignore>\?[^/]*)?((?<res>/[^/-]*)|((?<res>/[^/-]*)(?<view>-\w*)))?(?<cond>/[^/]*)?(?<meta>/[^/]*)?/?$""")]
    private static partial Regex RequestUriRegex();

    [GeneratedRegex("""^(?<key>[^\!=<>]*)(?<op>(=|\!=|<=|>=|<|>))(?<val>.*)$""")]
    private static partial Regex UriConditionRegex();
}
