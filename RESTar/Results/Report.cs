using System;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Returned to the client on successful REPORT requests
    /// </summary>
    public class Report : Content
    {
        /// <summary>
        /// The body of the report
        /// </summary>
        public ReportBody ReportBody { get; }

        /// <inheritdoc />
        public override Type EntityType => typeof(ReportBody);

        internal Report(IRequest request, long count) : base(request)
        {
            EntityCount = 1;
            ReportBody = new ReportBody(count);
        }
    };

    /// <summary>
    /// Describes the body of a report
    /// </summary>
    public class ReportBody
    {
        /// <summary>
        /// The number of entities counted
        /// </summary>
        public long Count { get; }

        internal ReportBody(long count) => Count = count;
    }
}