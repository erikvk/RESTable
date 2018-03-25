namespace RESTar.Results.Success
{
    /// <inheritdoc />
    /// <summary>
    /// Returned to the client on successful deletion of entities
    /// </summary>
    public class DeletedEntities : OK
    {
        /// <summary>
        /// The number of entities deleted
        /// </summary>
        public int DeletedCount { get; }

        internal DeletedEntities(int count, IQuery query) : base(query)
        {
            DeletedCount = count;
            Headers["RESTar-info"] = $"{count} entities deleted from '{query.Resource.Name}'";
            TimeElapsed = query.TimeElapsed;
        }
    }
}