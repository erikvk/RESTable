namespace RESTar.Results.Success
{
    /// <inheritdoc />
    /// <summary>
    /// A result that contains only status code, description and headers
    /// </summary>
    public class Head : OK
    {
        internal Head(IRequest request, long count) : base(request)
        {
            Headers["RESTar-count"] = count.ToString();
            TimeElapsed = request.TimeElapsed;
        }
    }
}