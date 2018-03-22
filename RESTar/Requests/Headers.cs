using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using RESTar.Linq;
using RESTar.Serialization.NativeProtocol;
using static System.StringComparison;

namespace RESTar.Requests
{
    /// <inheritdoc cref="IDictionary{TKey,TValue}" />
    /// <summary>
    /// A collection of request headers. Key comparison is case insensitive.
    /// </summary>
    [JsonConverter(typeof(HeadersConverter))]
    public class Headers : IDictionary<string, string>, IReadOnlyDictionary<string, string>
    {
        /// <summary>
        /// The Accept header
        /// </summary>
        public ContentType[] Accept { get; set; }

        /// <summary>
        /// The Content-Type header
        /// </summary>
        public ContentType? ContentType { get; set; }

        /// <summary>
        /// The Source header
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// The Destination header
        /// </summary>
        public string Destination { get; set; }

        /// <summary>
        /// The Origin header
        /// </summary>
        public string Origin { get; set; }

        /// <summary>
        /// The Authorization header
        /// </summary>
        public string Authorization { internal get; set; }

        internal bool UnsafeOverride { get; set; }

        /// <inheritdoc cref="IDictionary{TKey,TValue}" />
        /// <summary>
        /// Gets the header with the given name, or null if there is 
        /// no such header.
        /// </summary>
        public string this[string key]
        {
            get
            {
                switch (key)
                {
                    case var _ when key.EqualsNoCase(nameof(Accept)): return Accept.ToString();
                    case var _ when key.EqualsNoCase(nameof(ContentType)): return ContentType.ToString();
                    case var _ when key.EqualsNoCase(nameof(Source)): return Source;
                    case var _ when key.EqualsNoCase(nameof(Destination)): return Destination;
                    case var _ when key.EqualsNoCase(nameof(Authorization)): return Authorization;
                    case var _ when key.EqualsNoCase(nameof(Origin)): return Origin;
                    case var _ when _dict.TryGetValue(key, out var value): return value;
                    default: return default;
                }
            }
            set
            {
                switch (key)
                {
                    case var _ when key.EqualsNoCase(nameof(Accept)):
                        Accept = RESTar.ContentType.ParseManyOutput(value);
                        break;
                    case var _ when key.EqualsNoCase(nameof(ContentType)):
                        ContentType = RESTar.ContentType.ParseInput(value);
                        break;
                    case var _ when key.EqualsNoCase(nameof(Source)):
                        Source = value;
                        break;
                    case var _ when key.EqualsNoCase(nameof(Destination)):
                        Destination = value;
                        break;
                    case var _ when key.EqualsNoCase(nameof(Authorization)):
                        Authorization = value;
                        break;
                    case var _ when key.EqualsNoCase(nameof(Origin)):
                        Origin = value;
                        break;
                    default:
                        _dict[key] = value;
                        break;
                }
            }
        }

        private Dictionary<string, string> _dict { get; }
        private void Put(KeyValuePair<string, string> kvp) => _dict[kvp.Key] = kvp.Value;

        private IEnumerable<KeyValuePair<string, string>> ReservedHeaders => new[]
        {
            new KeyValuePair<string, string>(nameof(Accept), Accept.ToString()),
            new KeyValuePair<string, string>(nameof(ContentType), Accept.ToString()),
            new KeyValuePair<string, string>(nameof(Source), Source),
            new KeyValuePair<string, string>(nameof(Destination), Destination),
            new KeyValuePair<string, string>(nameof(Authorization), Authorization)
        };

        internal IEnumerable<KeyValuePair<string, string>> CustomHeaders => this
            .Union(ReservedHeaders)
            .Where(pair => IsCustom(pair.Key));

        internal static bool IsCustom(string key)
        {
            switch (key)
            {
                case var _ when string.Equals(key, "host", OrdinalIgnoreCase):
                case var _ when string.Equals(key, "authorization", OrdinalIgnoreCase):
                case var _ when string.Equals(key, "connection", OrdinalIgnoreCase):
                case var _ when string.Equals(key, "upgrade", OrdinalIgnoreCase):
                case var _ when string.Equals(key, "sec-websocket-version", OrdinalIgnoreCase):
                case var _ when string.Equals(key, "sec-websocket-key", OrdinalIgnoreCase):
                case var _ when string.Equals(key, "sec-websocket-extensions", OrdinalIgnoreCase):
                default: return true;
            }
        }

        IEnumerable<string> IReadOnlyDictionary<string, string>.Keys => Keys;
        IEnumerable<string> IReadOnlyDictionary<string, string>.Values => Values;

        /// <inheritdoc />
        public Headers()
        {
            _dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            ContentType = RESTar.ContentType.DefaultInput;
            Accept = null;
        }

        /// <inheritdoc />
        public Headers(Dictionary<string, string> dictToUse) : this() => dictToUse?.ForEach(Put);

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