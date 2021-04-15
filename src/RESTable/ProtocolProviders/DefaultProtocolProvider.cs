using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using RESTable.ContentTypeProviders;
using RESTable.Internal;
using RESTable.Linq;
using RESTable.Meta;
using RESTable.Requests;
using RESTable.Results;
using RESTable.WebSockets;
using Error = RESTable.Results.Error;

namespace RESTable.ProtocolProviders
{
    /// <inheritdoc />
    /// <summary>
    /// Contains the logic for the default RESTable protocol. This protocol is used if no 
    /// protocol indicator is included in the request URI.
    /// </summary>
    internal sealed class DefaultProtocolProvider : IProtocolProvider
    {
        /// <inheritdoc />
        public string ProtocolName { get; } = "RESTable";

        /// <inheritdoc />
        public string ProtocolIdentifier { get; } = "restable";

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
                case var _ when conditions.Length == 0:
                case var _ when conditions == "_": break;
                default:
                    ParseUriConditions(conditions, true).ForEach(uri.Conditions.Add);
                    break;
            }

            switch (metaConditions)
            {
                case var _ when metaConditions.Length == 0:
                case var _ when metaConditions == "_": break;
                default:
                    ParseUriConditions(metaConditions, true).ForEach(uri.MetaConditions.Add);
                    break;
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
                case var _macro when _macro.Substring(1) is string _: throw new MacrosNotSupported();
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

        public ExternalContentTypeProviderSettings ExternalContentTypeProviderSettings { get; } = ExternalContentTypeProviderSettings.AllowAll;

        public IEnumerable<IContentTypeProvider> GetCustomContentTypeProviders() => null;

        /// <inheritdoc />
        public string MakeRelativeUri(IUriComponents components) => ToUriString(components);

        private static string ToUriString(IUriComponents components)
        {
            var view = components.ViewName != null ? $"-{components.ViewName}" : null;
            var resource = components.Macro == null ? $"/{components.ResourceSpecifier}{view}" : $"/${components.Macro.Name}";
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

        internal static string ToUriString(IEnumerable<IUriCondition> conditions)
        {
            if (conditions == null) return "_";
            var uriString = string.Join("&", conditions.Select(ToUriString));
            if (uriString.Length == 0) return "_";
            return uriString;
        }

        private static string ToUriString(IUriCondition condition)
        {
            if (condition == null) return "";
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

        /// <inheritdoc />
        public async Task SerializeResult(ISerializedResult toSerialize, IContentTypeProvider contentTypeProvider, CancellationToken cancellationToken)
        {
            switch (toSerialize.Result)
            {
                case Head head:
                    head.Headers["EntityCount"] = head.EntityCount.ToString();
                    return;
                case Binary binary:
                    await binary.BinaryResult.WriteToStream(toSerialize.Body, cancellationToken).ConfigureAwait(false);
                    return;
                case IEntities<object> entities when entities is Content content:
                    await SerializeContentDataCollection((dynamic) entities, content, toSerialize, contentTypeProvider, cancellationToken).ConfigureAwait(false);
                    break;
                case Report report:
                    await SerializeContentDataCollection((dynamic) report.ToAsyncSingleton(), report, toSerialize, contentTypeProvider, cancellationToken).ConfigureAwait(false);
                    break;
                case Error error:
                    await SerializeError(error, toSerialize, contentTypeProvider, cancellationToken).ConfigureAwait(false);
                    break;
                default: return;
            }
        }

        private async Task SerializeError(Error error, ISerializedResult toSerialize, IContentTypeProvider contentTypeProvider, CancellationToken cancellationToken)
        {
            if (contentTypeProvider is not IJsonProvider)
                return;

            var swr = new StreamWriter(toSerialize.Body, Encoding.UTF8, 4096, true);
#if NETSTANDARD2_1
            await using (swr)
#else
            using (swr)
#endif
            {
                using var jwr = JsonProvider.GetJsonWriter(swr);

                await jwr.WriteStartObjectAsync(cancellationToken).ConfigureAwait(false);
                await jwr.WritePropertyNameAsync("Status", cancellationToken).ConfigureAwait(false);
                await jwr.WriteValueAsync("fail", cancellationToken).ConfigureAwait(false);
                await jwr.WritePropertyNameAsync("ErrorType", cancellationToken).ConfigureAwait(false);
                await jwr.WriteValueAsync(error.GetType().FullName, cancellationToken).ConfigureAwait(false);
                await jwr.WritePropertyNameAsync("Data", cancellationToken).ConfigureAwait(false);
                await jwr.WriteStartObjectAsync(cancellationToken).ConfigureAwait(false);
                if (error is FailedValidation failedValidation)
                {
                    foreach (var invalidMember in failedValidation.InvalidEntity.InvalidMembers)
                    {
                        await jwr.WritePropertyNameAsync(invalidMember.MemberName, cancellationToken).ConfigureAwait(false);
                        await jwr.WriteValueAsync(invalidMember.Message, cancellationToken).ConfigureAwait(false);
                    }
                    await jwr.WriteEndObjectAsync(cancellationToken).ConfigureAwait(false);
                    if (failedValidation.InvalidEntity.Index is long index)
                    {
                        await jwr.WritePropertyNameAsync("InvalidEntityIndex", cancellationToken).ConfigureAwait(false);
                        await jwr.WriteValueAsync(index, cancellationToken).ConfigureAwait(false);
                    }
                }
                else await jwr.WriteEndObjectAsync(cancellationToken).ConfigureAwait(false);
                await jwr.WritePropertyNameAsync("ErrorCode", cancellationToken).ConfigureAwait(false);
                await jwr.WriteValueAsync(error.ErrorCode.ToString(), cancellationToken).ConfigureAwait(false);
                await jwr.WritePropertyNameAsync("Message", cancellationToken).ConfigureAwait(false);
                await jwr.WriteValueAsync(error.Message, cancellationToken).ConfigureAwait(false);
                await jwr.WritePropertyNameAsync("MoreInfoAt", cancellationToken).ConfigureAwait(false);
                await jwr.WriteValueAsync(error.Headers.Error, cancellationToken).ConfigureAwait(false);
                await jwr.WritePropertyNameAsync("TimeElapsedMs", cancellationToken).ConfigureAwait(false);
                await jwr.WriteValueAsync(error.TimeElapsed.TotalMilliseconds, cancellationToken).ConfigureAwait(false);
                await jwr.WriteEndObjectAsync(cancellationToken).ConfigureAwait(false);
                toSerialize.EntityCount = 0;
            }
        }

        private async Task SerializeContentDataCollection<T>(IAsyncEnumerable<T> dataCollection, Content content, ISerializedResult toSerialize,
            IContentTypeProvider contentTypeProvider, CancellationToken cancellationToken) where T : class
        {
            content.SetContentDisposition(contentTypeProvider.ContentDispositionFileExtension);

            if (contentTypeProvider is not IJsonProvider jsonProvider)
            {
                await contentTypeProvider.SerializeCollection(dataCollection, toSerialize.Body, content.Request, cancellationToken).ConfigureAwait(false);
                return;
            }

            var swr = new StreamWriter(toSerialize.Body, Encoding.UTF8, 4096, true);
#if NETSTANDARD2_1
            await using (swr)
#else
            using (swr)
#endif
            {
                using var jwr = JsonProvider.GetJsonWriter(swr);
                await jwr.WriteStartObjectAsync(cancellationToken).ConfigureAwait(false);
                await jwr.WritePropertyNameAsync("Status", cancellationToken).ConfigureAwait(false);
                await jwr.WriteValueAsync("success", cancellationToken).ConfigureAwait(false);
                await jwr.WritePropertyNameAsync("ResourceType", cancellationToken).ConfigureAwait(false);
                await jwr.WriteValueAsync(content.ResourceType.FullName, cancellationToken).ConfigureAwait(false);
                await jwr.WritePropertyNameAsync("Data", cancellationToken).ConfigureAwait(false);
                var entityCount = await jsonProvider.SerializeCollection(dataCollection, jwr, cancellationToken).ConfigureAwait(false);
                toSerialize.EntityCount = entityCount;
                await jwr.WritePropertyNameAsync("DataCount", cancellationToken).ConfigureAwait(false);
                await jwr.WriteValueAsync(entityCount, cancellationToken).ConfigureAwait(false);
                if (content is IEntities entities)
                {
                    if (toSerialize.HasPreviousPage)
                    {
                        var previousPageLink = entities.GetPreviousPageLink(toSerialize.EntityCount);
                        await jwr.WritePropertyNameAsync("PreviousPage", cancellationToken).ConfigureAwait(false);
                        await jwr.WriteValueAsync(previousPageLink.ToUriString(), cancellationToken).ConfigureAwait(false);
                    }
                    if (toSerialize.HasNextPage)
                    {
                        var nextPageLink = entities.GetNextPageLink(toSerialize.EntityCount, -1);
                        await jwr.WritePropertyNameAsync("NextPage", cancellationToken).ConfigureAwait(false);
                        await jwr.WriteValueAsync(nextPageLink.ToUriString(), cancellationToken).ConfigureAwait(false);
                    }
                }
                await jwr.WritePropertyNameAsync("TimeElapsedMs", cancellationToken).ConfigureAwait(false);
                var milliseconds = Math.Round(toSerialize.TimeElapsed.TotalMilliseconds, 4);
                await jwr.WriteValueAsync(milliseconds, cancellationToken).ConfigureAwait(false);
                await jwr.WriteEndObjectAsync(cancellationToken).ConfigureAwait(false);
                if (entityCount == 0)
                    content.MakeNoContent();
            }
        }

        public bool IsCompliant(IRequest request, out string invalidReason)
        {
            invalidReason = null;
            return true;
        }

        public void OnInit() { }
    }
}