using System;
using RESTar.Operations;
using RESTar.Results.Error;

namespace RESTar.Results.Success
{
    internal class ReportBody
    {
        public long Count { get; set; }
    }

    /// <inheritdoc />
    /// <summary>
    /// A 200 result that encodes a content
    /// </summary>
    public class Content : OK
    {
        /// <summary>
        /// The request that requested this content
        /// </summary>
        public IRequest Request => RequestInternal;

        private IRequestInternal RequestInternal { get; }

        /// <inheritdoc />
        public Content(IRequest request) : base(request) => RequestInternal = (IRequestInternal) request;

        /// <inheritdoc />
        public override IFinalizedResult FinalizeResult()
        {
            try
            {
                return RequestInternal
                    .RequestParameters
                    .CachedProtocolProvider
                    .ProtocolProvider
                    .FinalizeResult(this, RequestInternal
                        .RequestParameters
                        .OutputContentTypeProvider);
            }
            catch (Exception exs)
            {
                return RequestInternal.HandleError(exs);
            }
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Returned to the client on successful REPORT requests
    /// </summary>
    public class Report : Content
    {
        internal IRequestInternal Request { get; }
        internal ReportBody ReportBody { get; }

        internal Report(IRequestInternal request, long count) : base(request)
        {
            Request = request;
            ReportBody = new ReportBody {Count = count};
        }
    }
}