using RESTar.Internal;

namespace RESTar.Results.Success
{
    public class UpdatedEntities : OK
    {
        internal UpdatedEntities(int count, IResource resource)
        {
            Headers["RESTar-info"] = $"{count} entities updated in resource '{resource.Name}'";
        }
    }
}