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
        #region Response headers

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

        #endregion

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
            get => this._Get(key);
            set => this._Set(key, value);
        }

        private IDictionary<string, string> _dict { get; }

        /// <inheritdoc />
        public Headers() => _dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <inheritdoc />
        public Headers(Dictionary<string, string> dictToUse) : this() => dictToUse?.ForEach(pair => this[pair.Key] = pair.Value);

        /// <inheritdoc />
        internal Headers(IHeadersInternal other) : this()
        {
            Accept = other.Accept;
            ContentType = other.ContentType;
            Source = other.Source;
            Destination = other.Destination;
            Authorization = other.Authorization;
            Origin = other.Origin;
            other.GetCustomHeaders().ForEach(pair => SetCustomHeader(pair.Key, pair.Value));
        }

        #region IHeadersInternal

        private void SetCustomHeader(string key, string value) => _dict[key] = value;
        bool IHeadersInternal.TryGetCustomHeader(string key, out string value) => _dict.TryGetValue(key, out value);
        void IHeadersInternal.SetCustomHeader(string key, string value) => SetCustomHeader(key, value);
        bool IHeadersInternal.ContainsCustomHeader(KeyValuePair<string, string> item) => _dict.Contains(item);
        bool IHeadersInternal.ContainsCustomHeaderName(string name) => _dict.ContainsKey(name);
        bool IHeadersInternal.RemoveCustomHeader(string name) => _dict.Remove(name);
        bool IHeadersInternal.RemoveCustomHeader(KeyValuePair<string, string> header) => _dict.Remove(header);
        IEnumerable<KeyValuePair<string, string>> IHeadersInternal.GetCustomHeaders() => _dict;

        #endregion

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => new HeadersEnumerator(this, _dict.GetEnumerator());

        /// <inheritdoc />
        void ICollection<KeyValuePair<string, string>>.Add(KeyValuePair<string, string> pair) => this._Set(pair.Key, pair.Value);

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
        public bool Contains(KeyValuePair<string, string> item) => this._Contains(item);

        /// <inheritdoc />
        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex) => this._CopyTo(array, arrayIndex);

        /// <inheritdoc />
        public bool Remove(KeyValuePair<string, string> item) => this._Remove(item);

        /// <inheritdoc cref="IDictionary{TKey,TValue}" />
        // ReSharper disable once UseCollectionCountProperty
        // We must use the HeadersEnumerator for this to work properly
        public int Count => this.Count();

        /// <inheritdoc />
        public bool IsReadOnly => false;

        /// <inheritdoc cref="IDictionary{TKey,TValue}" />
        public bool ContainsKey(string key) => this._ContainsKey(key);

        /// <inheritdoc />
        public void Add(string key, string value) => this._Set(key, value);

        /// <inheritdoc />
        public bool Remove(string key) => this._Remove(key);

        /// <inheritdoc cref="IDictionary{TKey,TValue}" />
        public bool TryGetValue(string key, out string value) => this._TryGetValue(key, out value);

        /// <inheritdoc />
        public ICollection<string> Keys => this._Keys();

        /// <inheritdoc />
        public ICollection<string> Values => this._Keys();
    }
}