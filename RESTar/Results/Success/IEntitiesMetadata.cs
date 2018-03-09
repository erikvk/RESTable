using RESTar.Requests;

namespace RESTar.Results.Success
{
    internal interface IEntitiesMetadata
    {
        ulong EntityCount { get; }
        string ResourceFullName { get; }

        /// <summary>
        /// Gets a link to the next set of entities, with a given number of entities to include
        /// </summary>
        IUriParameters GetNextPageLink(int count);

        /// <summary>
        /// Gets a link to the next set of entities, with the same amount of entities as in the last one
        /// </summary>
        IUriParameters GetNextPageLink();
    }
}