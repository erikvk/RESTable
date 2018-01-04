using RESTar.Internal;

namespace RESTar.Results.Success
{
    internal class SafePostedEntities : OK
    {
        internal SafePostedEntities(int upd, int ins, IResource resource)
        {
            Headers["RESTar-info"] = $"Updated {upd} and then inserted {ins} entities in resource '{resource.FullName}'";
        }
    }
}