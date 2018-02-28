using System.Collections.Generic;
using System.Linq;
using RESTar.Requests;

namespace RESTar.Admin
{
    /// <summary>
    /// Describes a content type
    /// </summary>
    public class ContentType
    {
        /// <summary>
        /// The MIME type of this content type
        /// </summary>
        public string MimeType { get; }

        /// <summary>
        /// Can this content type be used to read data?
        /// </summary>
        public bool CanRead { get; }

        /// <summary>
        /// Can this content type be used to write data?
        /// </summary>
        public bool CanWrite { get; }

        /// <summary>
        /// The MIME type string bindings used for the protocol provider
        /// </summary>
        public string[] Bindings { get; }

        internal ContentType(string mimeType, bool canRead, bool canWrite, string[] bindings)
        {
            MimeType = mimeType;
            CanRead = canRead;
            CanWrite = canWrite;
            Bindings = bindings;
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Contains all the available protocols and content types for the current RESTar instance
    /// </summary>
    [RESTar(Methods.GET)]
    public class Protocol : ISelector<Protocol>
    {
        private const string description = "Contains all the available protocols and content types for the current RESTar instance";

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
        public ContentType[] ContentTypes { get; private set; }

        /// <inheritdoc />
        public IEnumerable<Protocol> Select(IRequest<Protocol> request)
        {
            return RequestEvaluator.ProtocolProviders.Values.Distinct().Select(cachedProvider =>
            {
                var contentTypes = cachedProvider.InputMimeBindings.Values
                    .Union(cachedProvider.OutputMimeBindings.Values)
                    .Distinct()
                    .Select(provider => new ContentType
                    (
                        mimeType: provider.ContentType.MimeType,
                        canRead: cachedProvider.InputMimeBindings.Values.Contains(provider),
                        canWrite: cachedProvider.OutputMimeBindings.Values.Contains(provider),
                        bindings: cachedProvider.InputMimeBindings
                            .Where(binding => binding.Value.Equals(provider))
                            .Union(cachedProvider.OutputMimeBindings
                                .Where(binding => binding.Value.Equals(provider)))
                            .Select(binding => binding.Key).ToArray()
                    ));
                return new Protocol
                {
                    Name = cachedProvider.ProtocolProvider.ProtocolName,
                    Identifier = cachedProvider.ProtocolProvider.ProtocolIdentifier,
                    IsDefault = cachedProvider.ProtocolProvider is DefaultProtocolProvider,
                    ContentTypes = contentTypes.ToArray()
                };
            });
        }
    }
}