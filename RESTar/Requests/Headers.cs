using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using static System.StringComparison;

namespace RESTar.Requests
{
    /// <inheritdoc cref="IDictionary{TKey,TValue}" />
    /// <summary>
    /// A collection of request headers
    /// </summary>
    public class Headers : IDictionary<string, string>, IReadOnlyDictionary<string, string>
    {
        /// <inheritdoc cref="IDictionary{TKey,TValue}" />
        /// <summary>
        /// Gets the header with the given name, or null if there is 
        /// no such header.
        /// </summary>
        public string this[string key]
        {
            get
            {
                _dict.TryGetValue(key, out var value);
                return value;
            }
            set => _dict[key] = value;
        }

        internal bool UnsafeOverride { get; set; }

        private Dictionary<string, string> _dict { get; }
        internal void Put(KeyValuePair<string, string> kvp) => _dict[kvp.Key] = kvp.Value;
        internal IEnumerable<KeyValuePair<string, string>> CustomHeaders => this.Where(pair => IsCustom(pair.Key));

        internal static bool IsCustom(string key)
        {
            switch (key)
            {
                case var _ when string.Equals(key, "host", OrdinalIgnoreCase):
                case var _ when string.Equals(key, "authorization", OrdinalIgnoreCase):
                case var _ when string.Equals(key, "restar-authtoken", OrdinalIgnoreCase):
                case var _ when string.Equals(key, "connection", OrdinalIgnoreCase):
                case var _ when string.Equals(key, "upgrade", OrdinalIgnoreCase):
                case var _ when string.Equals(key, "sec-websocket-version", OrdinalIgnoreCase):
                case var _ when string.Equals(key, "sec-websocket-key", OrdinalIgnoreCase):
                case var _ when string.Equals(key, "sec-websocket-extensions", OrdinalIgnoreCase):
                case var _ when string.Equals(key, "source", OrdinalIgnoreCase):
                case var _ when string.Equals(key, "destination", OrdinalIgnoreCase): return false;
                default: return true;
            }
        }

        IEnumerable<string> IReadOnlyDictionary<string, string>.Keys => Keys;
        IEnumerable<string> IReadOnlyDictionary<string, string>.Values => Values;

        /// <inheritdoc />
        public Headers()
        {
            _dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        internal Headers(Dictionary<string, string> dictToUse)
        {
            if (dictToUse == null)
            {
                _dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                return;
            }
            _dict = dictToUse.Comparer.Equals(StringComparer.OrdinalIgnoreCase)
                ? dictToUse
                : new Dictionary<string, string>(dictToUse, StringComparer.OrdinalIgnoreCase);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _dict.GetEnumerator();

        /// <inheritdoc />
        void ICollection<KeyValuePair<string, string>>.Add(KeyValuePair<string, string> item) => Put(item);

        /// <inheritdoc />
        public void Clear() => throw new InvalidOperationException();

        /// <inheritdoc />
        public bool Contains(KeyValuePair<string, string> item) => ((IDictionary<string, string>) _dict).Contains(item);

        /// <inheritdoc />
        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex) => ((IDictionary<string, string>) _dict).CopyTo(array, arrayIndex);

        /// <inheritdoc />
        public bool Remove(KeyValuePair<string, string> item) => throw new InvalidOperationException();

        /// <inheritdoc cref="IDictionary{TKey,TValue}" />
        public int Count => _dict.Count;

        /// <inheritdoc />
        public bool IsReadOnly => ((IDictionary<string, string>) _dict).IsReadOnly;

        /// <inheritdoc cref="IDictionary{TKey,TValue}" />
        public bool ContainsKey(string key) => _dict.ContainsKey(key);

        /// <inheritdoc />
        public void Add(string key, string value) => _dict.Add(key, value);

        /// <inheritdoc />
        public bool Remove(string key) => throw new InvalidOperationException();

        /// <inheritdoc cref="IDictionary{TKey,TValue}" />
        public bool TryGetValue(string key, out string value) => _dict.TryGetValue(key, out value);

        /// <inheritdoc />
        public ICollection<string> Keys => _dict.Keys;

        /// <inheritdoc />
        public ICollection<string> Values => _dict.Values;
    }
}