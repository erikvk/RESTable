namespace RESTar.Results.Success
{
    /// <inheritdoc />
    /// <summary>
    /// Returned to the client on successful REPORT requests
    /// </summary>
    public class Report : Content
    {
        internal ReportBody ReportBody { get; }
        internal Report(IRequest request, long count) : base(request)
        {
            ReportBody = new ReportBody(count);
            TimeElapsed = request.TimeElapsed;
        }
    };

    internal class ReportBody
    {
        public long Count { get; }
        public ReportBody(long count) => Count = count;
    }
}