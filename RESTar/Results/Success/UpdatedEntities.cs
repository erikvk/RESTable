namespace RESTar.Results.Success
{
    /// <inheritdoc />
    /// <summary>
    /// Returned to the client on successful update of entities
    /// </summary>
    public class UpdatedEntities : OK
    {
        internal UpdatedEntities(int count, IRequest request) : base(request)
        {
            Headers["RESTar-info"] = $"{count} entities updated in '{request.Resource.Name}'";
        }
    }
}