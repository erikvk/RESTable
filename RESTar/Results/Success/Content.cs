using System;
using System.Diagnostics;
using System.Globalization;
using RESTar.Internal;
using RESTar.Requests;
using RESTar.Results.Error;

namespace RESTar.Results.Success
{
    /// <inheritdoc />
    /// <summary>
    /// A 200 result that encodes a content
    /// </summary>
    public abstract class Content : OK
    {
        /// <summary>
        /// The request that generated this content
        /// </summary>
        public IRequest Request => RequestInternal;

        /// <summary>
        /// The number of entities contained in this result
        /// </summary>
        public ulong EntityCount { get; set; }

        /// <summary>
        /// The type of entities contained in this result
        /// </summary>
        public abstract Type EntityType { get; }

        /// <summary>
        /// The content type of this result
        /// </summary>
        public new ContentType ContentType { get; private set; }

        private IRequestInternal RequestInternal { get; }

        /// <summary>
        /// Generates a URI string from URI components, according to the protocol of this Content
        /// </summary>
        protected string GetUriString(IUriComponents components) => RequestInternal
            .CachedProtocolProvider
            .ProtocolProvider
            .MakeRelativeUri(components);

        /// <inheritdoc />
        protected Content(IRequest request) : base(request) => RequestInternal = (IRequestInternal) request;

        /// <inheritdoc />
        public override ISerializedResult Serialize(ContentType? contentType = null)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var protocolProvider = RequestInternal.CachedProtocolProvider;
                var acceptProvider = ContentTypeController.ResolveOutputContentTypeProvider(RequestInternal, contentType);
                var streamController = new RESTarOutputStreamController();
                Body = streamController;
                ContentType = acceptProvider.ContentType;
                var serialized = protocolProvider.ProtocolProvider.Serialize(this, acceptProvider);
                if (serialized is Content content && content.Body is RESTarOutputStreamController rsc)
                    content.Body = rsc.Stream;
                return serialized;
            }
            catch (Exception exception)
            {
                var error = RESTarError.GetResult(exception, RequestInternal);
                return error.Serialize();
            }
            finally
            {
                stopwatch.Stop();
                TimeElapsed = TimeElapsed + stopwatch.Elapsed;
                Headers["RESTar-elapsed-ms"] = TimeElapsed.TotalMilliseconds.ToString(CultureInfo.InvariantCulture);
            }
        }
    }
}