using RESTable.Requests;

namespace RESTable.Results
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

        internal Report(IRequest request, ulong count) : base(request) => ReportBody = new ReportBody(count);

        /// <inheritdoc />
        public override string Metadata => $"{nameof(Report)};{Request.Resource};{ReportBody.Count}";
    }

    /// <summary>
    /// Describes the body of a report
    /// </summary>
    public class ReportBody
    {
        /// <summary>
        /// The number of entities counted
        /// </summary>
        public ulong Count { get; }

        internal ReportBody(ulong count) => Count = count;
    }
}