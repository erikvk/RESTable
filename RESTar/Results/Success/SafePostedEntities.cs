namespace RESTar.Results.Success
{
    /// <inheritdoc />
    /// <summary>
    /// Returned to the client on successful safe post insertion/updating
    /// </summary>
    public class SafePostedEntities : OK
    {
        internal SafePostedEntities(int upd, int ins, IRequest request) : base(request)
        {
            Headers["RESTar-info"] = $"Updated {upd} and then inserted {ins} entities in resource '{request.Resource.Name}'";
        }
    }
}