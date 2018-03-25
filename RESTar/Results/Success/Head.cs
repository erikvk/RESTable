namespace RESTar.Results.Success
{
    /// <inheritdoc />
    /// <summary>
    /// A result that contains only status code, description and headers
    /// </summary>
    public class Head : OK
    {
        internal Head(IQuery query, long count) : base(query)
        {
            Headers["RESTar-count"] = count.ToString();
            TimeElapsed = query.TimeElapsed;
        }
    }
}