using RESTar.Internal;

namespace RESTar.Results.Success
{
    internal class DeletedEntities : OK
    {
        internal DeletedEntities(int count, IEntityResource resource)
        {
            Headers["RESTar-info"] = $"{count} entities deleted from resource '{resource.FullName}'";
        }
    }
}