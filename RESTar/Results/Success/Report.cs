namespace RESTar.Results.Success
{
    internal class ReportBody
    {
        public long Count { get; set; }
    }

    /// <inheritdoc />
    /// <summary>
    /// Returned to the client on successful REPORT requests
    /// </summary>
    public class Report : OK
    {
        internal IRequest Request { get; }
        internal ReportBody ReportBody { get; }

        internal Report(IRequest request, long count) : base(request)
        {
            Request = request;
            ReportBody = new ReportBody {Count = count};
        }
    }
}