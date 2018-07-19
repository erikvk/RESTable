using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using RESTar.ContentTypeProviders.NativeJsonProtocol;
using RESTar.Linq;

namespace RESTar.Requests
{
    /// <inheritdoc cref="IDictionary{TKey,TValue}" />
    /// <summary>
    /// A collection of request headers. Key comparison is case insensitive.
    /// </summary>
    [JsonConverter(typeof(HeadersConverter<Headers>))]
    public class Headers : IHeaders, IHeadersInternal
    {
        internal static HashSet<string> NonCustomHeaders { get; }

        static Headers() => NonCustomHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "host", "authorization", "connection", "upgrade", "restar-metadata", "sec-websocket-version", "sec-websocket-key",
            "sec-websocket-extensions"
        };

        internal string Info
        {
            get => this["RESTar-info"];
            set => this["RESTar-info"] = value;
        }

        internal string Error
        {
            get => this["RESTar-error"];
            set => this["RESTar-error"] = value;
        }

        internal string Elapsed
        {
            get => this["RESTar-elapsed-ms"];
            set => this["RESTar-elapsed-ms"] = value;
        }

        internal string EntityCount
        {
            get => this["RESTar-count"];
            set => this["RESTar-count"] = value;
        }

        internal string Pager
        {
            get => this["RESTar-pager"];
            set => this["RESTar-pager"] = value;
        }

        internal string Metadata
        {
            get => this["RESTar-metadata"];
            set => this["RESTar-metadata"] = value;
        }

        internal string Version
        {
            get => this["RESTar-version"];
            set => this["RESTar-version"] = value;
        }

        /// <inheritdoc />
        public ContentTypes Accept { get; set; }

        /// <inheritdoc />
        public ContentType? ContentType { get; set; }

        /// <inheritdoc />
        public string Source { get; set; }

        /// <inheritdoc />
        public string Destination { get; set; }

        /// <summary>
        /// The Authorization header
        /// </summary>
        public string Authorization { internal get; set; }

        string IHeaders.Authorization
        {
            get => Authorization;
            set => Authorization = value;
        }

        /// <inheritdoc />
        public string Origin { get; set; }

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
                    case var _ when key.EqualsNoCase(nameof(Accept)): return Accept?.ToString();
                    case var _ when key.EqualsNoCase("Content-Type"): return ContentType?.ToString();
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
                        if (!string.IsNullOrWhiteSpace(value))
                            Accept = Requests.ContentType.ParseMany(value);
                        break;
                    case var _ when key.EqualsNoCase("Content-Type"):
                        if (!string.IsNullOrWhiteSpace(value))
                            ContentType = Requests.ContentType.Parse(value);
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

        IEnumerable<KeyValuePair<string, string>> IHeadersInternal.GetCustom(HashSet<string> whitelist) => GetCustom(whitelist);

        internal IEnumerable<KeyValuePair<string, string>> GetCustom(HashSet<string> whitelist = null)
        {
            return this.Where(pair => whitelist?.Contains(pair.Key) == true || IsCustom(pair.Key));
        }

        internal static bool IsCustom(string key) => !NonCustomHeaders.Contains(key);

        /// <inheritdoc />
        public Headers() => _dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <inheritdoc />
        public Headers(Dictionary<string, string> dictToUse) : this() => dictToUse?.ForEach(Put);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => new HeadersEnumerator(this, _dict.GetEnumerator());

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
                case var _ when item.Key.EqualsNoCase(nameof(Accept)): return Accept?.ToString().EqualsNoCase(item.Value) == true;
                case var _ when item.Key.EqualsNoCase("Content-Type"): return ContentType?.ToString().EqualsNoCase(item.Value) == true;
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
        // ReSharper disable once UseCollectionCountProperty
        // We must use the HeadersEnumerator for this to work properly
        public int Count => this.Count();

        /// <inheritdoc />
        public bool IsReadOnly => false;

        /// <inheritdoc cref="IDictionary{TKey,TValue}" />
        public bool ContainsKey(string key)
        {
            switch (key)
            {
                case var _ when key.EqualsNoCase(nameof(Accept)): return Accept != null;
                case var _ when key.EqualsNoCase("Content-Type"): return ContentType != null;
                case var _ when key.EqualsNoCase(nameof(Source)): return Source != null;
                case var _ when key.EqualsNoCase(nameof(Destination)): return Destination != null;
                case var _ when key.EqualsNoCase(nameof(Authorization)): return Authorization != null;
                case var _ when key.EqualsNoCase(nameof(Origin)): return Origin != null;
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
                    value = Accept?.ToString();
                    return value != null;
                case var _ when key.EqualsNoCase("Content-Type"):
                    value = ContentType?.ToString();
                    return value != null;
                case var _ when key.EqualsNoCase(nameof(Source)):
                    value = Source;
                    return value != null;
                case var _ when key.EqualsNoCase(nameof(Destination)):
                    value = Destination;
                    return value != null;
                case var _ when key.EqualsNoCase(nameof(Authorization)):
                    value = Authorization;
                    return value != null;
                case var _ when key.EqualsNoCase(nameof(Origin)):
                    value = Origin;
                    return value != null;
                default: return _dict.TryGetValue(key, out value);
            }
        }

        /// <inheritdoc />
        public ICollection<string> Keys => this.Select(kvp => kvp.Key).ToList();

        /// <inheritdoc />
        public ICollection<string> Values => this.Select(kvp => kvp.Value).ToList();
    }
}