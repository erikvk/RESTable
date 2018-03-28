using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
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
                if (contentType.HasValue)
                    contentType = contentType.Value;
                else if (!(RequestInternal.Headers.Accept?.Count > 0))
                    contentType = protocolProvider.DefaultOutputProvider.ContentType;
                IContentTypeProvider acceptProvider = null;
                if (!contentType.HasValue)
                {
                    var containedWildcard = false;
                    var foundProvider = RequestInternal.Headers.Accept.Any(a =>
                    {
                        if (!a.AnyType)
                            return protocolProvider.OutputMimeBindings.TryGetValue(a.MimeType, out acceptProvider);
                        containedWildcard = true;
                        return false;
                    });
                    if (!foundProvider)
                        if (containedWildcard)
                            acceptProvider = protocolProvider.DefaultOutputProvider;
                        else
                            throw new NotAcceptable(RequestInternal.Headers.Accept.ToString());
                }
                else if (!protocolProvider.OutputMimeBindings.TryGetValue(contentType.Value.MimeType, out acceptProvider))
                    throw new NotAcceptable(contentType.Value.ToString());

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
                return RESTarError.GetResult(exception, RequestInternal);
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