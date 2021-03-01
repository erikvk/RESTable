using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using RESTable.Internal;
using RESTable.ProtocolProviders;
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
        public string Name { get; private set; }

        /// <summary>
        /// The identifier of the protocol
        /// </summary>
        public string Identifier { get; private set; }

        /// <summary>
        /// Is this the default protocol?
        /// </summary>
        public bool IsDefault { get; private set; }

        /// <summary>
        /// The content types supported by this protocol
        /// </summary>
        public IEnumerable<ContentTypeInfo> ContentTypes { get; private set; }

        /// <inheritdoc />
        public IEnumerable<Protocol> Select(IRequest<Protocol> request) => request
            .GetService<ProtocolController>()
            .CachedProtocolProviders
            .Values
            .Distinct()
            .Select(cachedProvider => new Protocol
            {
                Name = cachedProvider.ProtocolProvider.ProtocolName,
                Identifier = cachedProvider.ProtocolProvider.ProtocolIdentifier,
                IsDefault = cachedProvider.ProtocolProvider is DefaultProtocolProvider,
                ContentTypes = cachedProvider.InputMimeBindings.Values
                    .Union(cachedProvider.OutputMimeBindings.Values)
                    .Distinct()
                    .Select(provider => new ContentTypeInfo
                    {
                        Name = provider.Name,
                        MimeType = provider.ContentType.MediaType,
                        CanRead = cachedProvider.InputMimeBindings.Values.Contains(provider),
                        CanWrite = cachedProvider.OutputMimeBindings.Values.Contains(provider),
                        Bindings = cachedProvider.InputMimeBindings
                            .Where(binding => binding.Value.Equals(provider))
                            .Union(cachedProvider.OutputMimeBindings
                                .Where(binding => binding.Value.Equals(provider)))
                            .Select(binding => binding.Key)
                    })
            });
    }
}