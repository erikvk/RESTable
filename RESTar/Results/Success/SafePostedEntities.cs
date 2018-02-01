namespace RESTar.Results.Success
{
    internal class SafePostedEntities : OK
    {
        internal SafePostedEntities(int upd, int ins, IRequest request) : base(request)
        {
            Headers["RESTar-info"] = $"Updated {upd} and then inserted {ins} entities in resource '{request.Resource.Name}'";
        }
    }
}