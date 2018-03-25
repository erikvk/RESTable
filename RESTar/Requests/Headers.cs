using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RESTar.Linq;

namespace RESTar.Queries
{
    internal class HeadersEnumerator : IEnumerator<KeyValuePair<string, string>>
    {
        private HeadersMembers CurrentMember;
        private readonly Headers Headers;
        private readonly IEnumerator<KeyValuePair<string, string>> DictEnumerator;
        public void Dispose() => Reset();
        public void Reset() => CurrentMember = HeadersMembers.nil;
        object IEnumerator.Current => Current;
        public KeyValuePair<string, string> Current { get; private set; }

        public bool MoveNext()
        {
            switch (CurrentMember += 1)
            {
                case HeadersMembers.Accept:
                    Current = new KeyValuePair<string, string>(nameof(Headers.Accept), Headers.Accept?.ToString());
                    break;
                case HeadersMembers.ContentType:
                    Current = new KeyValuePair<string, string>("Content-Type", Headers.ContentType?.ToString());
                    break;
                case HeadersMembers.Source:
                    Current = new KeyValuePair<string, string>(nameof(Headers.Source), Headers.ContentType?.ToString());
                    break;
                case HeadersMembers.Destination:
                    Current = new KeyValuePair<string, string>(nameof(Headers.Destination), Headers.ContentType?.ToString());
                    break;
                case HeadersMembers.Authorization:
                    Current = new KeyValuePair<string, string>(nameof(Headers.Authorization), Headers.ContentType?.ToString());
                    break;
                case HeadersMembers.Origin:
                    Current = new KeyValuePair<string, string>(nameof(Headers.Origin), Headers.ContentType?.ToString());
                    break;
                default:
                    if (!DictEnumerator.MoveNext())
                        return false;
                    Current = DictEnumerator.Current;
                    return true;
            }
            return Current.Value != null || MoveNext();
        }

        public HeadersEnumerator(Headers headers, IEnumerator<KeyValuePair<string, string>> dictEnumerator)
        {
            Headers = headers;
            DictEnumerator = dictEnumerator;
        }

        private enum HeadersMembers
        {
            nil = 0,
            Accept,
            ContentType,
            Source,
            Destination,
            Authorization,
            Origin
        }
    }

    /// <inheritdoc cref="IDictionary{TKey,TValue}" />
    /// <summary>
    /// A collection of request headers. Key comparison is case insensitive.
    /// </summary>
    //[JsonConverter(typeof(HeadersConverter))]
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
        /// The Authorization header
        /// </summary>
        public string Authorization { internal get; set; }

        /// <summary>
        /// The Origin header
        /// </summary>
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

        internal IEnumerable<KeyValuePair<string, string>> CustomHeaders => this.Where(pair => IsCustom(pair.Key));

        internal static bool IsCustom(string key)
        {
            switch (key)
            {
                case var _ when key.EqualsNoCase("host"):
                case var _ when key.EqualsNoCase("authorization"):
                case var _ when key.EqualsNoCase("connection"):
                case var _ when key.EqualsNoCase("upgrade"):
                case var _ when key.EqualsNoCase("sec-websocket-version"):
                case var _ when key.EqualsNoCase("sec-websocket-key"):
                case var _ when key.EqualsNoCase("sec-websocket-extensions"):
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