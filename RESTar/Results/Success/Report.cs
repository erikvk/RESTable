namespace RESTar.Results.Success
{
    /// <inheritdoc />
    /// <summary>
    /// Returned to the client on successful REPORT requests
    /// </summary>
    public class Report : Content
    {
        internal ReportBody ReportBody { get; }
        internal Report(IQuery query, long count) : base(query)
        {
            ReportBody = new ReportBody(count);
            TimeElapsed = query.TimeElapsed;
        }
    };

    internal class ReportBody
    {
        public long Count { get; }
        public ReportBody(long count) => Count = count;
    }
}