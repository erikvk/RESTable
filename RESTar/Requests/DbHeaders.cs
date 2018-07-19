using System.Collections.Generic;
using System.Linq;
using Dynamit;
using Newtonsoft.Json;
using RESTar.ContentTypeProviders.NativeJsonProtocol;
using Starcounter;

namespace RESTar.Requests
{
    /// <inheritdoc cref="DDictionary" />
    /// <inheritdoc cref="IDDictionary{TTable,TKvp}" />
    /// <inheritdoc cref="IHeaders" />
    /// <summary>
    /// A persistent storage for request headers
    /// </summary>
    [Database, JsonConverter(typeof(HeadersConverter<DbHeaders>))]
    public class DbHeaders : DDictionary, IDDictionary<DbHeaders, DbHeadersKvp>, IHeaders, IHeadersInternal
    {
        /// <inheritdoc />
        public ContentTypes Accept { get; set; }

        /// <inheritdoc />
        public ContentType? ContentType { get; set; }

        /// <inheritdoc />
        public string Source { get; set; }

        /// <inheritdoc />
        public string Destination { get; set; }

        /// <inheritdoc />
        public string Authorization { get; set; }

        /// <inheritdoc />
        public string Origin { get; set; }

        #region Explicit implementations

        IEnumerable<KeyValuePair<string, string>> IHeadersInternal.GetCustom(HashSet<string> whitelist) => KeyValuePairs
            .Where(pair => whitelist?.Contains(pair.Key) == true || !Headers.NonCustomHeaders.Contains(pair.Key))
            .Select(pair => new KeyValuePair<string, string>(pair.Key, pair.ValueString));

        void ICollection<KeyValuePair<string, string>>.Add(KeyValuePair<string, string> item) => Add(item.Key, item.Value);
        void IDictionary<string, string>.Add(string key, string value) => Add(key, value);
        ICollection<string> IDictionary<string, string>.Values => Values.Select(value => value.ToString()).ToList();

        DbHeadersKvp IDDictionary<DbHeaders, DbHeadersKvp>.NewKeyPair(DbHeaders dict, string key, object value) =>
            new DbHeadersKvp(dict, key, value?.ToString());

        IEnumerator<KeyValuePair<string, string>> IEnumerable<KeyValuePair<string, string>>.GetEnumerator() =>
            KeyValuePairs.Select(p => new KeyValuePair<string, string>(p.Key, p.Value?.ToString())).GetEnumerator();

        bool ICollection<KeyValuePair<string, string>>.Contains(KeyValuePair<string, string> item) =>
            Contains(new KeyValuePair<string, object>(item.Key, item.Value));

        bool ICollection<KeyValuePair<string, string>>.Remove(KeyValuePair<string, string> item) =>
            Remove(new KeyValuePair<string, object>(item.Key, item.Value));

        void ICollection<KeyValuePair<string, string>>.CopyTo(KeyValuePair<string, string>[] array, int arrayIndex) =>
            CopyTo(array
                .Select(pair => new KeyValuePair<string, object>(pair.Key, pair.Value))
                .ToArray(), arrayIndex);

        bool IDictionary<string, string>.TryGetValue(string key, out string value)
        {
            if (TryGetValue(key, out var _value))
            {
                value = _value?.ToString();
                return true;
            }
            value = null;
            return false;
        }

        string IDictionary<string, string>.this[string key]
        {
            get => this[key]?.ToString();
            set => this[key] = value;
        }

        #endregion
    }

    /// <inheritdoc />
    /// <summary>
    /// Key-value pair class for DbHeaders
    /// </summary>
    [Database]
    public class DbHeadersKvp : DKeyValuePair
    {
        /// <inheritdoc />
        public DbHeadersKvp(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }
}