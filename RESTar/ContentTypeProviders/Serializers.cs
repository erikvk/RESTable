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
        public static readonly Json Json;

        /// <summary>
        /// A statically accessable ExcelContentProvider
        /// </summary>
        public static readonly Excel Excel;

        static Serializers()
        {
            Json = new Json();
            Excel = new Excel();
        }
    }
}