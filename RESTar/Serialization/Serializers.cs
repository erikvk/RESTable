using RESTar.ContentTypeProviders;

namespace RESTar.Serialization
{
    /// <summary>
    /// The serializer for the RESTar instance
    /// </summary>
    public static class Serializers
    {
        /// <summary>
        /// A statically accessable JsonContentProvider
        /// </summary>
        public static readonly JsonContentProvider Json;

        /// <summary>
        /// A statically accessable ExcelContentProvider
        /// </summary>
        public static readonly ExcelContentProvider Excel;

        static Serializers()
        {
            Json = new JsonContentProvider();
            Excel = new ExcelContentProvider();
        }
    }
}