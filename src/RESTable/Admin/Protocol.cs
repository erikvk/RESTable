using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using RESTable.DefaultProtocol;
using RESTable.Internal;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Resources.Operations;

namespace RESTable.Admin
{
    /// <inheritdoc />
    /// <summary>
    /// Contains all the available protocols and content types for the current RESTable instance
    /// </summary>
    [RESTable(Method.GET, Description = description)]
    public class Protocol : ISelector<Protocol>
    {
        private const string description = "Contains all the available protocols and content types for the current RESTable instance";

        /// <summary>
        /// The protocol name
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The identifier of the protocol
        /// </summary>
        public string Identifier { get; }

        /// <summary>
        /// Is this the default protocol?
        /// </summary>
        public bool IsDefault { get; }

        /// <summary>
        /// The content types supported by this protocol
        /// </summary>
        public IEnumerable<ContentTypeInfo> ContentTypes { get; }

        public Protocol(string name, string identifier, bool isDefault, IEnumerable<ContentTypeInfo> contentTypes)
        {
            Name = name;
            Identifier = identifier;
            IsDefault = isDefault;
            ContentTypes = contentTypes;
        }

        /// <inheritdoc />
        public IEnumerable<Protocol> Select(IRequest<Protocol> request) => request
            .GetRequiredService<ProtocolProviderManager>()
            .CachedProtocolProviders
            .Values
            .Distinct()
            .Select(cachedProvider => new Protocol
            (
                name: cachedProvider.ProtocolProvider.ProtocolName,
                identifier: cachedProvider.ProtocolProvider.ProtocolIdentifier,
                isDefault: cachedProvider.ProtocolProvider is DefaultProtocolProvider,
                contentTypes: cachedProvider.InputMimeBindings.Values
                    .Union(cachedProvider.OutputMimeBindings.Values)
                    .Distinct()
                    .Select(provider => new ContentTypeInfo
                    (
                        name: provider.Name,
                        mimeType: provider.ContentType.MediaType,
                        canRead: cachedProvider.InputMimeBindings.Values.Contains(provider),
                        canWrite: cachedProvider.OutputMimeBindings.Values.Contains(provider),
                        bindings: cachedProvider.InputMimeBindings
                            .Where(binding => binding.Value.Equals(provider))
                            .Union(cachedProvider.OutputMimeBindings
                                .Where(binding => binding.Value.Equals(provider)))
                            .Select(binding => binding.Key)
                    ))
            ));
    }
}