namespace RESTar.Resources
{
    /// <summary>
    /// Helper used for fetching the domin of a given ResourceProvider type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct Provider<T> where T : ResourceProvider
    {
        /// <summary>
        /// The domain of the given ResourceProvider type
        /// </summary>
        public static string Get => typeof(T).FullName;
    }
}