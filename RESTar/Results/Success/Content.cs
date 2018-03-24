using System;
using System.Diagnostics;
using System.Linq;
using RESTar.Operations;
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

        private IRequestInternal RequestInternal { get; }

        /// <inheritdoc />
        protected Content(IRequest request) : base(request) => RequestInternal = (IRequestInternal) request;

        /// <inheritdoc />
        public override IFinalizedResult FinalizeResult(ContentType? contentType = null)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var protocolProvider = RequestInternal.ProtocolProvider;
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
                else
                {
                    if (!protocolProvider.OutputMimeBindings.TryGetValue(contentType.Value.MimeType, out acceptProvider))
                        throw new NotAcceptable(contentType.Value.ToString());
                }
                return protocolProvider.ProtocolProvider.FinalizeResult(this, acceptProvider);
            }
            catch (Exception exception)
            {
                return RESTarError.GetResult(exception, RequestInternal);
            }
            finally
            {
                stopwatch.Stop();
                TimeElapsed = TimeElapsed + stopwatch.Elapsed;
            }
        }
    }
}