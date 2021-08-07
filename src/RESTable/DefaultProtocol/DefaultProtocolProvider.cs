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
using Error = RESTable.Results.Error;

namespace RESTable.DefaultProtocol
{
    /// <inheritdoc />
    /// <summary>
    /// Contains the logic for the default RESTable protocol. This protocol is used if no 
    /// protocol indicator is included in the request URI.
    /// </summary>
    internal sealed class DefaultProtocolProvider : IProtocolProvider
    {
        /// <inheritdoc />
        public string ProtocolName => "RESTable";

        /// <inheritdoc />
        public string ProtocolIdentifier => "restable";

        /// <inheritdoc />
        public IUriComponents GetUriComponents(string uriString, RESTableContext context)
        {
            var uri = new DefaultProtocolUriComponents(this);
            var match = Regex.Match(uriString, RegEx.RESTableRequestUri);
            if (!match.Success) throw new InvalidSyntax(ErrorCodes.InvalidUriSyntax, "Check URI syntax");
            var resourceOrMacro = match.Groups["res"].Value.TrimStart('/');
            var view = match.Groups["view"].Value.TrimStart('-');
            var conditions = match.Groups["cond"].Value.TrimStart('/');
            var metaConditions = match.Groups["meta"].Value.TrimStart('/');

            switch (conditions)
            {
                case string { Length: 0 }:
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
                case string { Length: 0 }:
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

        internal static List<IUriCondition> ParseUriConditions(string conditionsString, bool check = false) => conditionsString
            .Split('&')
            .Select(s => ParseUriCondition(s, check))
            .ToList();

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
            var match = Regex.Match(conditionString, RegEx.UriCondition);
            if (!match.Success) throw new InvalidSyntax(ErrorCodes.InvalidConditionSyntax, $"Invalid condition syntax at '{conditionString}'");
            var (key, opString, valueLiteral) = (match.Groups["key"].Value, match.Groups["op"].Value, match.Groups["val"].Value);
            if (!Operator.TryParse(opString, out var @operator)) throw new InvalidOperator(conditionString);
            return new UriCondition
            (
                key: key.UriDecode(),
                op: @operator.OpCode,
                valueLiteral: valueLiteral.UriDecode(),
                valueTypeCode: TypeCode.String
            );
        }

        private IJsonProvider JsonProvider { get; }
        private ResourceCollection ResourceCollection { get; }

        public DefaultProtocolProvider(IJsonProvider jsonProvider, ResourceCollection resourceCollection)
        {
            ResourceCollection = resourceCollection;
            JsonProvider = jsonProvider;
        }

        public ExternalContentTypeProviderSettings ExternalContentTypeProviderSettings => ExternalContentTypeProviderSettings.AllowAll;

        public IEnumerable<IContentTypeProvider>? GetCustomContentTypeProviders() => null;

        /// <inheritdoc />
        public string MakeRelativeUri(IUriComponents components) => ToUriString(components);

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
                str.Append($"/_/{UnescapeMetaconditions(ToUriString(components.MetaConditions))}");
            return str.ToString();
        }

        private static string UnescapeMetaconditions(string metaconditionsString) => metaconditionsString
            .Replace("%2C", ",")
            .Replace("%3E", ">")
            .Replace("%24", "$");

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

        public void SetResultHeaders(IResult result) { }

        /// <inheritdoc />
        public async Task SerializeResult(ISerializedResult toSerialize, IContentTypeProvider contentTypeProvider, CancellationToken cancellationToken)
        {
            switch (toSerialize.Result)
            {
                case Options options:
                    await SerializeOptions(options, toSerialize, contentTypeProvider, cancellationToken).ConfigureAwait(false);
                    break;
                case Head head:
                    head.Headers["EntityCount"] = head.EntityCount.ToString();
                    return;
                case Change change:
                    await SerializeChange((dynamic) change, toSerialize, contentTypeProvider, cancellationToken).ConfigureAwait(false);
                    return;
                case Binary binary:
                    await binary.BinaryResult.WriteToStream(toSerialize.Body, cancellationToken).ConfigureAwait(false);
                    return;
                case IEntities<object> entities:
                    await SerializeEntities((dynamic) entities, toSerialize, contentTypeProvider, cancellationToken).ConfigureAwait(false);
                    break;
                case Report report:
                    await SerializeReport(report, toSerialize, contentTypeProvider, cancellationToken).ConfigureAwait(false);
                    break;
                case Error error:
                    await SerializeError(error, toSerialize, contentTypeProvider, cancellationToken).ConfigureAwait(false);
                    break;
                default: return;
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
            if (error.Request.UriComponents is IUriComponents uriComponents)
            {
                uri = ToUriString(uriComponents);
            }
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
                var count = await contentTypeProvider.SerializeCollectionAsync(toSerialize.Body, entities, cancellationToken).ConfigureAwait(false);
                toSerialize.EntityCount = count;
                return;
            }
#if NET6_0_OR_GREATER
            var serializedContent = new SerializedEntitiesAsyncEnumerable<T>(entities, toSerialize);
#else
            var serializedContent = new SerializedEntitiesEnumerable<T>(entities, toSerialize);
#endif
            await jsonProvider.SerializeAsync(toSerialize.Body, serializedContent, cancellationToken: cancellationToken).ConfigureAwait(false);
            toSerialize.EntityCount = serializedContent.DataCount;
        }

        private static Task SerializeChange<T>(Change<T> change, ISerializedResult toSerialize, IContentTypeProvider contentTypeProvider, CancellationToken cancellationToken)
            where T : class
        {
            toSerialize.EntityCount = change.Count;
            if (contentTypeProvider is not IJsonProvider jsonProvider)
                return Task.CompletedTask;

            var serializedChange = new SerializedChange<T>(change);
            return jsonProvider.SerializeAsync(toSerialize.Body, serializedChange, cancellationToken: cancellationToken);
        }


        private static Task SerializeReport(Report report, ISerializedResult toSerialize, IContentTypeProvider contentTypeProvider, CancellationToken cancellationToken)
        {
            toSerialize.EntityCount = 1;
            if (contentTypeProvider is not IJsonProvider jsonProvider)
                return Task.CompletedTask;
            var serializedReport = new SerializedReport(report);
            return jsonProvider.SerializeAsync(toSerialize.Body, serializedReport, cancellationToken: cancellationToken);
        }


        public bool IsCompliant(IRequest request, out string? invalidReason)
        {
            invalidReason = null;
            return true;
        }

        public void OnInit() { }
    }
}