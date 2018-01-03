using RESTar.Internal;

namespace RESTar.Results.Success
{
    internal class DeletedEntities : OK
    {
        internal DeletedEntities(int count, IResource resource)
        {
            Headers["RESTar-info"] = $"{count} entities deleted from resource '{resource.Name}'";
        }
    }
}