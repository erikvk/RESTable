namespace RESTar.ContentTypeProviders
{
    /// <summary>
    /// The serializer for the RESTar instance
    /// </summary>
    public static class Serializers
    {
        /// <summary>
        /// A statically accessable JsonContentProvider
        /// </summary>
        public static readonly JsonProvider JsonProvider;

        /// <summary>
        /// A statically accessable ExcelContentProvider
        /// </summary>
        public static readonly ExcelProvider ExcelProvider;

        static Serializers()
        {
            JsonProvider = new JsonProvider();
            ExcelProvider = new ExcelProvider();
        }
    }
}