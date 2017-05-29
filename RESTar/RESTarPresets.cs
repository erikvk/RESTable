namespace RESTar
{
    public enum RESTarPresets : byte
    {
        /// <summary>
        /// Makes GET available for this resource
        /// </summary>
        ReadOnly,

        /// <summary>
        /// Makes POST and DELETE available for this resource
        /// </summary>
        WriteOnly,

        /// <summary>
        /// Makes GET and PATCH available for this resource
        /// </summary>
        ReadAndUpdate,

        /// <summary>
        /// Makes all methods available for this resource
        /// </summary>
        ReadAndWrite
    }
}