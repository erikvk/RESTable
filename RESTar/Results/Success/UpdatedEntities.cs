using RESTar.Internal;

namespace RESTar.Results.Success
{
    internal class UpdatedEntities : OK
    {
        internal UpdatedEntities(int count, IEntityResource resource)
        {
            Headers["RESTar-info"] = $"{count} entities updated in resource '{resource.FullName}'";
        }
    }
}