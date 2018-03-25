using System;
using System.Diagnostics;
using System.Linq;
using RESTar.Operations;
using RESTar.Queries;
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
        public IQuery Query => QueryInternal;

        private IQueryInternal QueryInternal { get; }

        /// <summary>
        /// Generates a URI string from URI components, according to the protocol of this Content
        /// </summary>
        protected string GetUriString(IUriComponents components) => QueryInternal
            .CachedProtocolProvider
            .ProtocolProvider
            .MakeRelativeUri(components);

        /// <inheritdoc />
        protected Content(IQuery query) : base(query) => QueryInternal = (IQueryInternal) query;

        /// <inheritdoc />
        public override ISerializedResult Serialize(ContentType? contentType = null)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var protocolProvider = QueryInternal.CachedProtocolProvider;
                if (contentType.HasValue)
                    contentType = contentType.Value;
                else if (!(QueryInternal.Headers.Accept?.Count > 0))
                    contentType = protocolProvider.DefaultOutputProvider.ContentType;
                IContentTypeProvider acceptProvider = null;
                if (!contentType.HasValue)
                {
                    var containedWildcard = false;
                    var foundProvider = QueryInternal.Headers.Accept.Any(a =>
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
                            throw new NotAcceptable(QueryInternal.Headers.Accept.ToString());
                }
                else
                {
                    if (!protocolProvider.OutputMimeBindings.TryGetValue(contentType.Value.MimeType, out acceptProvider))
                        throw new NotAcceptable(contentType.Value.ToString());
                }
                return protocolProvider.ProtocolProvider.Serialize(this, acceptProvider);
            }
            catch (Exception exception)
            {
                return RESTarError.GetResult(exception, QueryInternal);
            }
            finally
            {
                stopwatch.Stop();
                TimeElapsed = TimeElapsed + stopwatch.Elapsed;
            }
        }
    }
}