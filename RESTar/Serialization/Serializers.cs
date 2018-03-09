using RESTar.ContentTypeProviders;

namespace RESTar.Serialization
{
    /// <summary>
    /// The serializer for the RESTar instance
    /// </summary>
    public static class Serializers
    {
        // private static readonly JsonSerializerSettings VmSettings;

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
            // VmSettings = new JsonSerializerSettings
            // {
            //     ContractResolver = new CreateViewModelResolver(),
            //     DateFormatHandling = DateFormatHandling.IsoDateFormat,
            //     DateTimeZoneHandling = DateTimeZoneHandling.Utc
            // };
            // VmSettings.Converters.Add(enumConverter);
            // VmSettings.Converters.Add(headersConverter);
            // VmSettings.Converters.Add(ddictionaryConverter);
            Json = new JsonContentProvider();
            Excel = new ExcelContentProvider();
        }

        #region Main serializers

        #endregion


        // internal static string SerializeToViewModel(this object value)
        // {
        //     return JsonConvert.SerializeObject(value, VmSettings);
        // }
    }
}