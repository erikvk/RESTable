using System.Collections.Generic;

namespace RESTar.Requests
{
    /// <summary>
    /// A collection of request headers
    /// </summary>
    public class Headers
    {
        /// <summary>
        /// Gets the header with the given name, or null if there is 
        /// no such header.
        /// </summary>
        public string this[string key]
        {
            get
            {
                dict.TryGetValue(key, out var value);
                return value;
            }
        }

        private readonly Dictionary<string, string> dict;
        internal void Add(KeyValuePair<string, string> kvp) => dict[kvp.Key] = kvp.Value;
        internal Headers() => dict = new Dictionary<string, string>();
        internal Headers(Dictionary<string, string> dictToUse) => dict = dictToUse ?? new Dictionary<string, string>();
    }
}