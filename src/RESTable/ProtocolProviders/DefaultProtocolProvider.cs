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
        private const int StreamWriterBufferSize = 4096;

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
                case string {Length: 0}:
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
                case string {Length: 0}:
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

        public ExternalContentTypeProviderSettings ExternalContentTypeProviderSettings => ExternalContentTypeProviderSettings.AllowAll;

        public IEnumerable<IContentTypeProvider> GetCustomContentTypeProviders() => null;

        /// <inheritdoc />
        public string MakeRelativeUri(IUriComponents components) => ToUriString(components);

        private static string ToUriString(IUriComponents components)
        {
            var view = components.ViewName != null ? $"-{components.ViewName}" : null;
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

        internal static string ToUriString(IEnumerable<IUriCondition> conditions)
        {
            if (conditions is null) return "_";
            var uriString = string.Join("&", conditions.Select(ToUriString));
            if (uriString.Length == 0) return "_";
            return uriString;
        }

        private static string ToUriString(IUriCondition condition)
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
                case IEntities<object> entities and Content content:
                    await SerializeContentDataCollection((dynamic) entities, content, toSerialize, contentTypeProvider, cancellationToken).ConfigureAwait(false);
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

        private async Task SerializeOptions(Options options, ISerializedResult toSerialize, IContentTypeProvider contentTypeProvider, CancellationToken cancellationToken)
        {
            if (contentTypeProvider is not IJsonProvider jsonProvider)
                return;
            if (options.Resource is not IResource resource)
                return;
            var optionsBody = new OptionsBody(resource.Name, resource.ResourceKind, resource.AvailableMethods);

            var swr = new StreamWriter(toSerialize.Body, Encoding.UTF8, StreamWriterBufferSize, true);
#if NETSTANDARD2_1
            await using (swr.ConfigureAwait(false))
#else
            using (swr)
#endif
            {
                using var jwr = JsonProvider.GetJsonWriter(swr);
                await jwr.WriteStartObjectAsync(cancellationToken).ConfigureAwait(false);
                await jwr.WritePropertyNameAsync("Status", cancellationToken).ConfigureAwait(false);
                await jwr.WriteValueAsync("success", cancellationToken).ConfigureAwait(false);
                await jwr.WritePropertyNameAsync("Data", cancellationToken).ConfigureAwait(false);
                var entityCount = await jsonProvider.SerializeCollection(optionsBody.ToAsyncSingleton(), jwr, cancellationToken).ConfigureAwait(false);
                toSerialize.EntityCount = entityCount;
                await jwr.WritePropertyNameAsync("DataCount", cancellationToken).ConfigureAwait(false);
                await jwr.WriteValueAsync(entityCount, cancellationToken).ConfigureAwait(false);
                await jwr.WriteEndObjectAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task SerializeError(Error error, ISerializedResult toSerialize, IContentTypeProvider contentTypeProvider, CancellationToken cancellationToken)
        {
            if (contentTypeProvider is not IJsonProvider)
                return;

            var swr = new StreamWriter(toSerialize.Body, Encoding.UTF8, StreamWriterBufferSize, true);
#if NETSTANDARD2_1
            await using (swr.ConfigureAwait(false))
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
                if (error is InvalidInputEntity failedValidation)
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
                await jwr.WritePropertyNameAsync("TimeStamp", cancellationToken).ConfigureAwait(false);
                await jwr.WriteValueAsync(DateTime.UtcNow.ToString("O"), cancellationToken).ConfigureAwait(false);
                if (error.Request?.UriComponents is IUriComponents uriComponents)
                {
                    await jwr.WritePropertyNameAsync("Uri", cancellationToken).ConfigureAwait(false);
                    await jwr.WriteValueAsync(ToUriString(uriComponents), cancellationToken).ConfigureAwait(false);
                }
                await jwr.WritePropertyNameAsync("TimeElapsedMs", cancellationToken).ConfigureAwait(false);
                var milliseconds = toSerialize.TimeElapsed.GetRESTableElapsedMs();
                await jwr.WriteValueAsync(milliseconds, cancellationToken).ConfigureAwait(false);
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

            var swr = new StreamWriter(toSerialize.Body, Encoding.UTF8, StreamWriterBufferSize, true);
#if NETSTANDARD2_1
            await using (swr.ConfigureAwait(false))
#else
            using (swr)
#endif
            {
                using var jwr = JsonProvider.GetJsonWriter(swr);
                await jwr.WriteStartObjectAsync(cancellationToken).ConfigureAwait(false);
                await jwr.WritePropertyNameAsync("Status", cancellationToken).ConfigureAwait(false);
                await jwr.WriteValueAsync("success", cancellationToken).ConfigureAwait(false);
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
                var milliseconds = toSerialize.TimeElapsed.GetRESTableElapsedMs();
                await jwr.WriteValueAsync(milliseconds, cancellationToken).ConfigureAwait(false);
                await jwr.WriteEndObjectAsync(cancellationToken).ConfigureAwait(false);
                if (entityCount == 0)
                    content.MakeNoContent();
            }
        }

        private async Task SerializeChange<T>(Change<T> change, ISerializedResult toSerialize, IContentTypeProvider contentTypeProvider, CancellationToken cancellationToken)
            where T : class
        {
            if (contentTypeProvider is not IJsonProvider jsonProvider)
                return;

            var swr = new StreamWriter(toSerialize.Body, Encoding.UTF8, StreamWriterBufferSize, true);
#if NETSTANDARD2_1
            await using (swr.ConfigureAwait(false))
#else
            using (swr)
#endif
            {
                using var jwr = JsonProvider.GetJsonWriter(swr);
                await jwr.WriteStartObjectAsync(cancellationToken).ConfigureAwait(false);
                await jwr.WritePropertyNameAsync("Status", cancellationToken).ConfigureAwait(false);
                await jwr.WriteValueAsync("success", cancellationToken).ConfigureAwait(false);
                await jwr.WritePropertyNameAsync("Data", cancellationToken).ConfigureAwait(false);
                var entityCount = await jsonProvider.SerializeCollection(change.Entities.ToAsyncEnumerable(), jwr, cancellationToken).ConfigureAwait(false);
                toSerialize.EntityCount = entityCount;
                await jwr.WritePropertyNameAsync("DataCount", cancellationToken).ConfigureAwait(false);
                await jwr.WriteValueAsync(entityCount, cancellationToken).ConfigureAwait(false);
                if (change.TooManyEntities)
                {
                    await jwr.WritePropertyNameAsync("TooManyEntitiesToIncludeInBody", cancellationToken).ConfigureAwait(false);
                    await jwr.WriteValueAsync(true, cancellationToken).ConfigureAwait(false);
                }

                switch (change)
                {
                    case UpdatedEntities<T>:
                    {
                        await jwr.WritePropertyNameAsync("UpdatedCount", cancellationToken).ConfigureAwait(false);
                        await jwr.WriteValueAsync(change.Count, cancellationToken).ConfigureAwait(false);
                        break;
                    }
                    case InsertedEntities<T>:
                    {
                        await jwr.WritePropertyNameAsync("InsertedCount", cancellationToken).ConfigureAwait(false);
                        await jwr.WriteValueAsync(change.Count, cancellationToken).ConfigureAwait(false);
                        break;
                    }
                    case DeletedEntities<T>:
                    {
                        await jwr.WritePropertyNameAsync("DeletedCount", cancellationToken).ConfigureAwait(false);
                        await jwr.WriteValueAsync(change.Count, cancellationToken).ConfigureAwait(false);
                        break;
                    }
                    case SafePostedEntities<T> spe:
                    {
                        await jwr.WritePropertyNameAsync("UpdatedCount", cancellationToken).ConfigureAwait(false);
                        await jwr.WriteValueAsync(spe.UpdatedCount, cancellationToken).ConfigureAwait(false);
                        await jwr.WritePropertyNameAsync("InsertedCount", cancellationToken).ConfigureAwait(false);
                        await jwr.WriteValueAsync(spe.InsertedCount, cancellationToken).ConfigureAwait(false);
                        break;
                    }
                }

                await jwr.WritePropertyNameAsync("TimeElapsedMs", cancellationToken).ConfigureAwait(false);
                var milliseconds = toSerialize.TimeElapsed.GetRESTableElapsedMs();
                await jwr.WriteValueAsync(milliseconds, cancellationToken).ConfigureAwait(false);
                await jwr.WriteEndObjectAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task SerializeReport(Report report, ISerializedResult toSerialize, IContentTypeProvider contentTypeProvider, CancellationToken cancellationToken)
        {
            if (contentTypeProvider is not IJsonProvider jsonProvider)
                return;

            var swr = new StreamWriter(toSerialize.Body, Encoding.UTF8, StreamWriterBufferSize, true);
#if NETSTANDARD2_1
            await using (swr.ConfigureAwait(false))
#else
            using (swr)
#endif
            {
                using var jwr = JsonProvider.GetJsonWriter(swr);
                await jwr.WriteStartObjectAsync(cancellationToken).ConfigureAwait(false);
                await jwr.WritePropertyNameAsync("Status", cancellationToken).ConfigureAwait(false);
                await jwr.WriteValueAsync("success", cancellationToken).ConfigureAwait(false);
                await jwr.WritePropertyNameAsync("Count", cancellationToken).ConfigureAwait(false);
                await jwr.WriteValueAsync(report.Count, cancellationToken).ConfigureAwait(false);
                await jwr.WritePropertyNameAsync("TimeElapsedMs", cancellationToken).ConfigureAwait(false);
                var milliseconds = toSerialize.TimeElapsed.GetRESTableElapsedMs();
                await jwr.WriteValueAsync(milliseconds, cancellationToken).ConfigureAwait(false);
                await jwr.WriteEndObjectAsync(cancellationToken).ConfigureAwait(false);
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