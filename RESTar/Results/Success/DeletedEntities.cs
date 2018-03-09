namespace RESTar.Results.Success
{
    /// <inheritdoc />
    /// <summary>
    /// Returned to the client on successful deletion of entities
    /// </summary>
    public class DeletedEntities : OK
    {
        internal DeletedEntities(int count, IRequest request) : base(request)
        {
            Headers["RESTar-info"] = $"{count} entities deleted from '{request.Resource.Name}'";
        }
    }
}