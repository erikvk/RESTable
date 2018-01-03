using System;
using System.Collections;
using System.Collections.Generic;

namespace RESTar.Requests
{
    /// <inheritdoc />
    /// <summary>
    /// A collection of request headers
    /// </summary>
    public class Headers : IDictionary<string, string>
    {
        /// <inheritdoc />
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
            set => dict[key] = value;
        }

        private readonly Dictionary<string, string> dict;
        internal void Put(KeyValuePair<string, string> kvp) => dict[kvp.Key] = kvp.Value;
        internal Headers() => dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        internal Headers(Dictionary<string, string> dictToUse)
        {
            if (dictToUse == null)
            {
                dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                return;
            }
            dict = dictToUse.Comparer.Equals(StringComparer.OrdinalIgnoreCase)
                ? dictToUse
                : new Dictionary<string, string>(dictToUse, StringComparer.OrdinalIgnoreCase);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => dict.GetEnumerator();

        /// <inheritdoc />
        void ICollection<KeyValuePair<string, string>>.Add(KeyValuePair<string, string> item) => Put(item);

        /// <inheritdoc />
        public void Clear() => dict.Clear();

        /// <inheritdoc />
        public bool Contains(KeyValuePair<string, string> item) => ((IDictionary<string, string>) dict).Contains(item);

        /// <inheritdoc />
        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex) => ((IDictionary<string, string>) dict).CopyTo(array, arrayIndex);

        /// <inheritdoc />
        public bool Remove(KeyValuePair<string, string> item) => ((IDictionary<string, string>) dict).Remove(item);

        /// <inheritdoc />
        public int Count => dict.Count;

        /// <inheritdoc />
        public bool IsReadOnly => ((IDictionary<string, string>) dict).IsReadOnly;

        /// <inheritdoc />
        public bool ContainsKey(string key) => dict.ContainsKey(key);

        /// <inheritdoc />
        public void Add(string key, string value) => dict.Add(key, value);

        /// <inheritdoc />
        public bool Remove(string key) => dict.Remove(key);

        /// <inheritdoc />
        public bool TryGetValue(string key, out string value) => dict.TryGetValue(key, out value);

        /// <inheritdoc />
        public ICollection<string> Keys => dict.Keys;

        /// <inheritdoc />
        public ICollection<string> Values => dict.Values;
    }
}