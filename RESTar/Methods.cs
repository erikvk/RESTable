namespace RESTar
{
    /// <summary>
    /// The REST methods available in RESTar
    /// </summary>
    public enum Methods
    {
        /// <summary>
        /// GET, returns entities from a resource
        /// </summary>
        GET,

        /// <summary>
        /// POST, inserts entities into a resource
        /// </summary>
        POST,

        /// <summary>
        /// PATCH, updates existing entities in a resource
        /// </summary>
        PATCH,

        /// <summary>
        /// PUT, tries to locate a resource entity. If no one was found, 
        /// inserts a new entity. If one was found, updates that entity.
        /// If more than one was found, returns an error.
        /// </summary>
        PUT,

        /// <summary>
        /// DELETE, deletes one or more entities from a resource
        /// </summary>
        DELETE,

        /// <summary>
        /// COUNT, returns the number of entities contained in a GET 
        /// response from a resource. Enabling GET for a resource automatically 
        /// enables COUNT for that resource.
        /// </summary>
        REPORT
    }
}