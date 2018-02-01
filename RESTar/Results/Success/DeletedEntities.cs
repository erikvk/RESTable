namespace RESTar.Results.Success
{
    internal class DeletedEntities : OK
    {
        internal DeletedEntities(int count, IRequest request) : base(request)
        {
            Headers["RESTar-info"] = $"{count} entities deleted from '{request.Resource.Name}'";
        }
    }
}