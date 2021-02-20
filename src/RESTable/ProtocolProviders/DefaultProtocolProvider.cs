using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RESTable.Admin;
using RESTable.ContentTypeProviders;
using RESTable.ContentTypeProviders.NativeJsonProtocol;
using RESTable.Internal;
using RESTable.Linq;
using RESTable.Meta;
using RESTable.Requests;
using RESTable.Results;
using RESTable.WebSockets;

namespace RESTable.ProtocolProviders
{
    internal class DefaultProtocolUriComponents : IUriComponents
    {
        public string ResourceSpecifier { get; internal set; }
        public string ViewName { get; internal set; }
        public IMacro Macro { get; internal set; }
        IReadOnlyCollection<IUriCondition> IUriComponents.Conditions => Conditions;
        IReadOnlyCollection<IUriCondition> IUriComponents.MetaConditions => MetaConditions;
        public List<IUriCondition> Conditions { get; }
        public List<IUriCondition> MetaConditions { get; }
        public IProtocolProvider ProtocolProvider { get; }

        public DefaultProtocolUriComponents(IProtocolProvider protocolProvider)
        {
            ProtocolProvider = protocolProvider;
            Conditions = new List<IUriCondition>();
            MetaConditions = new List<IUriCondition>();
        }
    }

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
                        ? Shell.TerminalResource.Name
                        : EntityResource<AvailableResource>.ResourceSpecifier;
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

        public void SetResultHeaders(IResult result)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public async Task SerializeResult(ISerializedResult toSerialize, IContentTypeProvider contentTypeProvider)
        {
            switch (toSerialize.Result)
            {
                case Head head:
                    head.Headers["EntityCount"] = head.EntityCount.ToString();
                    return;
                case Binary binary:
                    await binary.BinaryResult.WriteToStream(toSerialize.Body).ConfigureAwait(false);
                    return;
                case IEntities<object> entities when entities is Content content:
                    await SerializeContentDataCollection((dynamic) entities, content, toSerialize, contentTypeProvider).ConfigureAwait(false);
                    break;
                case Report report:
                    await SerializeContentDataCollection((dynamic) report.ToAsyncSingleton(), report, toSerialize, contentTypeProvider).ConfigureAwait(false);
                    break;
                default: return;
            }
        }

        private async Task SerializeContentDataCollection<T>(IAsyncEnumerable<T> dataCollection, Content content, ISerializedResult toSerialize, IContentTypeProvider contentTypeProvider) where T : class
        {
            content.SetContentDisposition(contentTypeProvider.ContentDispositionFileExtension);

            if (contentTypeProvider is not IJsonProvider jsonProvider)
            {
                await contentTypeProvider.SerializeCollection(dataCollection, toSerialize.Body, content.Request).ConfigureAwait(false);
                return;
            }

            await using var swr = new StreamWriter(toSerialize.Body, Encoding.UTF8, 1024, true);
            using var jwr = new RESTableJsonWriter(swr, 0) {Formatting = Settings._PrettyPrint ? Formatting.Indented : Formatting.None};
            await jwr.WriteStartObjectAsync().ConfigureAwait(false);
            await jwr.WritePropertyNameAsync("ResourceType").ConfigureAwait(false);
            await jwr.WriteValueAsync(content.ResourceType.FullName).ConfigureAwait(false);
            if (Settings._PrettyPrint)
                await jwr.WriteIndentationAsync().ConfigureAwait(false);
            await jwr.WriteRawAsync("\"Data\": ").ConfigureAwait(false);
            await jwr.FlushAsync().ConfigureAwait(false);
            var entityCount = await jsonProvider.SerializeCollection
            (
                collectionObject: dataCollection,
                stream: toSerialize.Body,
                baseIndentation: 2,
                request: content.Request
            ).ConfigureAwait(false);
            toSerialize.EntityCount = entityCount;
            await jwr.WritePropertyNameAsync("DataCount").ConfigureAwait(false);
            await jwr.WriteValueAsync(entityCount).ConfigureAwait(false);
            await jwr.WritePropertyNameAsync("Status").ConfigureAwait(false);
            await jwr.WriteValueAsync("success").ConfigureAwait(false);
            if (toSerialize.HasPreviousPage)
            {
                var previousPageLink = toSerialize.GetPreviousPageLink();
                await jwr.WritePropertyNameAsync("PreviousPage").ConfigureAwait(false);
                await jwr.WriteValueAsync(previousPageLink.ToUriString()).ConfigureAwait(false);
            }
            if (toSerialize.HasNextPage)
            {
                var nextPageLink = toSerialize.GetNextPageLink();
                await jwr.WritePropertyNameAsync("NextPage").ConfigureAwait(false);
                await jwr.WriteValueAsync(nextPageLink.ToUriString()).ConfigureAwait(false);
            }
            await jwr.WritePropertyNameAsync("TimeElapsedMs").ConfigureAwait(false);
            await jwr.WriteValueAsync(toSerialize.TimeElapsed.TotalMilliseconds).ConfigureAwait(false);
            await jwr.WriteEndObjectAsync().ConfigureAwait(false);
            if (entityCount == 0)
                content.MakeNoContent();
        }

        public bool IsCompliant(IRequest request, out string invalidReason)
        {
            invalidReason = null;
            return true;
        }

        public void OnInit() { }
    }
}