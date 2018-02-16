using RESTar.Requests;

namespace RESTar.Results.Success
{
    /// <summary>
    /// A result that contains only status code, description and headers
    /// </summary>
    public class Head : OK
    {
        internal Head(ITraceable trace, long count) : base(trace)
        {
            Headers["RESTar-count"] = count.ToString();
        }
    }
}