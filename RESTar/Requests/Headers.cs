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
        public ContentTypes Accept { get; set; }

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
                    case var _ when key.EqualsNoCase("Content-Type"): return ContentType.ToString();
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
                    case var _ when key.EqualsNoCase("Content-Type"):
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
        private void Put(KeyValuePair<string, string> kvp) => this[kvp.Key] = kvp.Value;
        private void Put(string key, string value) => this[key] = value;

        private IEnumerable<KeyValuePair<string, string>> ReservedHeaders => new[]
        {
            new KeyValuePair<string, string>(nameof(Accept), Accept.ToString()),
            new KeyValuePair<string, string>(nameof(ContentType), Accept.ToString()),
            new KeyValuePair<string, string>(nameof(Source), Source),
            new KeyValuePair<string, string>(nameof(Destination), Destination),
            new KeyValuePair<string, string>(nameof(Authorization), Authorization)
        };

        private readonly string[] ReservedHeaderKeys =
            {nameof(Accept), "Content-Type", nameof(Source), nameof(Destination), nameof(Authorization), nameof(Origin)};

        private IEnumerable<string> ReservedHeaderValues => new[]
            {Accept.ToString(), ContentType.ToString(), Source, Destination, Authorization, Origin};

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
        public Headers() => _dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <inheritdoc />
        public Headers(Dictionary<string, string> dictToUse) : this() => dictToUse?.ForEach(Put);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _dict.Union(ReservedHeaders).GetEnumerator();

        /// <inheritdoc />
        void ICollection<KeyValuePair<string, string>>.Add(KeyValuePair<string, string> item) => Put(item);

        /// <inheritdoc />
        public void Clear()
        {
            Accept = null;
            ContentType = null;
            Source = null;
            Destination = null;
            Authorization = null;
            Origin = null;
            _dict.Clear();
        }

        /// <inheritdoc />
        public bool Contains(KeyValuePair<string, string> item)
        {
            switch (item.Key)
            {
                case var _ when item.Key.EqualsNoCase(nameof(Accept)): return Accept.ToString().EqualsNoCase(item.Value);
                case var _ when item.Key.EqualsNoCase("Content-Type"): return ContentType.ToString().EqualsNoCase(item.Value);
                case var _ when item.Key.EqualsNoCase(nameof(Source)): return Source.EqualsNoCase(item.Value);
                case var _ when item.Key.EqualsNoCase(nameof(Destination)): return Destination.EqualsNoCase(item.Value);
                case var _ when item.Key.EqualsNoCase(nameof(Authorization)): return Authorization.EqualsNoCase(item.Value);
                case var _ when item.Key.EqualsNoCase(nameof(Origin)): return Origin.EqualsNoCase(item.Value);
                default: return _dict.Contains(item);
            }
        }

        /// <inheritdoc />
        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex) => ((IDictionary<string, string>) _dict).CopyTo(array, arrayIndex);

        /// <inheritdoc />
        public bool Remove(KeyValuePair<string, string> item) => Remove(item.Key);

        /// <inheritdoc cref="IDictionary{TKey,TValue}" />
        public int Count => _dict.Count + ReservedHeaderKeys.Length;

        /// <inheritdoc />
        public bool IsReadOnly => false;

        /// <inheritdoc cref="IDictionary{TKey,TValue}" />
        public bool ContainsKey(string key)
        {
            switch (key)
            {
                case var _ when key.EqualsNoCase(nameof(Accept)):
                case var _ when key.EqualsNoCase("Content-Type"):
                case var _ when key.EqualsNoCase(nameof(Source)):
                case var _ when key.EqualsNoCase(nameof(Destination)):
                case var _ when key.EqualsNoCase(nameof(Authorization)):
                case var _ when key.EqualsNoCase(nameof(Origin)): return true;
                default: return _dict.ContainsKey(key);
            }
        }

        /// <inheritdoc />
        public void Add(string key, string value) => Put(key, value);

        /// <inheritdoc />
        public bool Remove(string key)
        {
            switch (key)
            {
                case var _ when key.EqualsNoCase(nameof(Accept)):
                    Accept = null;
                    return true;
                case var _ when key.EqualsNoCase("Content-Type"):
                    ContentType = null;
                    return true;
                case var _ when key.EqualsNoCase(nameof(Source)):
                    Source = null;
                    return true;
                case var _ when key.EqualsNoCase(nameof(Destination)):
                    Destination = null;
                    return true;
                case var _ when key.EqualsNoCase(nameof(Authorization)):
                    Authorization = null;
                    return true;
                case var _ when key.EqualsNoCase(nameof(Origin)):
                    Origin = null;
                    return true;
                default: return _dict.Remove(key);
            }
        }

        /// <inheritdoc cref="IDictionary{TKey,TValue}" />
        public bool TryGetValue(string key, out string value)
        {
            switch (key)
            {
                case var _ when key.EqualsNoCase(nameof(Accept)):
                    value = Accept.ToString();
                    return true;
                case var _ when key.EqualsNoCase("Content-Type"):
                    value = ContentType.ToString();
                    return true;
                case var _ when key.EqualsNoCase(nameof(Source)):
                    value = Source;
                    return true;
                case var _ when key.EqualsNoCase(nameof(Destination)):
                    value = Destination;
                    return true;
                case var _ when key.EqualsNoCase(nameof(Authorization)):
                    value = Authorization;
                    return true;
                case var _ when key.EqualsNoCase(nameof(Origin)):
                    value = ContentType.ToString();
                    return true;
                default: return _dict.TryGetValue(key, out value);
            }
        }

        /// <inheritdoc />
        public ICollection<string> Keys => _dict.Keys.Union(ReservedHeaderKeys).ToList();

        /// <inheritdoc />
        public ICollection<string> Values => _dict.Values.Union(ReservedHeaderValues).ToList();
    }
}