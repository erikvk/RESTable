using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Primitives;

namespace RESTable.Requests;

/// <inheritdoc cref="IDictionary{TKey,TValue}" />
/// <summary>
///     A collection of request headers. Key comparison is case insensitive.
/// </summary>
public sealed class Headers : IHeaders, IHeadersInternal, IDictionary
{
    public Headers()
    {
        _dict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public Headers(IDictionary<string, string>? dictToUse) : this()
    {
        if (dictToUse is null) return;
        foreach (var (key, value) in dictToUse)
            this[key] = value;
    }

    public Headers(IDictionary<string, StringValues>? dictToUse) : this()
    {
        if (dictToUse is null) return;
        foreach (var (key, value) in dictToUse)
            this[key] = value;
    }

    /// <inheritdoc />
    private Headers(IHeadersInternal other) : this()
    {
        Accept = other.Accept;
        ContentType = other.ContentType;
        Source = other.Source;
        Destination = other.Destination;
        Authorization = other.Authorization;
        Origin = other.Origin;
        Elapsed = other.Elapsed;
        foreach (var (key, value) in other.GetCustomHeaders())
            SetCustomHeader(key, value);
    }

    internal bool UnsafeOverride { get; set; }

    private IDictionary<string, string?> _dict { get; }

    public void CopyTo(Array array, int index)
    {
        throw new NotImplementedException();
    }

    public bool IsSynchronized => false;
    public object SyncRoot => null!;

    public void Add(object key, object? value)
    {
        this._Set(key.ToString()!, value?.ToString());
    }

    public bool Contains(object key)
    {
        return this._ContainsKey(key.ToString()!);
    }

    IDictionaryEnumerator IDictionary.GetEnumerator()
    {
        return new Enumerator(GetHeaderEnumeration().GetEnumerator());
    }

    public void Remove(object key)
    {
        this._Remove(key.ToString()!);
    }

    public bool IsFixedSize => false;

    public object? this[object key]
    {
        get => this[key.ToString()!];
        set => this[key.ToString()!] = value?.ToString();
    }

    ICollection IDictionary.Keys => this._Keys();
    ICollection IDictionary.Values => this._Values();

    /// <inheritdoc />
    public ContentTypes? Accept { get; set; }

    /// <inheritdoc />
    public ContentType? ContentType { get; set; }

    /// <inheritdoc />
    public string? Source { get; set; }

    /// <inheritdoc />
    public string? Destination { get; set; }

    /// <inheritdoc />
    public TimeSpan? Elapsed { get; set; }

    /// <summary>
    ///     The Authorization header
    /// </summary>
    public string? Authorization { get; set; }

    /// <inheritdoc />
    public string? Origin { get; set; }

    /// <inheritdoc cref="IDictionary{TKey,TValue}" />
    /// <summary>
    ///     Gets the header with the given name, or null if there is
    ///     no such header.
    /// </summary>
    public string? this[string key]
    {
        get => this._Get(key);
        set
        {
            if (value is null) Remove(key);
            this._Set(key, value);
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<string, string?>> GetEnumerator()
    {
        return GetHeaderEnumeration().GetEnumerator();
        // new HeadersEnumerator(this, _dict.GetEnumerator());
    }

    /// <inheritdoc />
    void ICollection<KeyValuePair<string, string?>>.Add(KeyValuePair<string, string?> pair)
    {
        this._Set(pair.Key, pair.Value);
    }

    public void Clear()
    {
        Accept = null;
        ContentType = null;
        Source = null;
        Destination = null;
        Authorization = null;
        Origin = null;
        Elapsed = null;
        _dict.Clear();
    }

    /// <inheritdoc />
    public bool Contains(KeyValuePair<string, string?> item)
    {
        return this._Contains(item);
    }

    /// <inheritdoc />
    public void CopyTo(KeyValuePair<string, string?>[] array, int arrayIndex)
    {
        this._CopyTo(array, arrayIndex);
    }

    /// <inheritdoc />
    public bool Remove(KeyValuePair<string, string?> item)
    {
        return this._Remove(item);
    }

    /// <inheritdoc cref="IDictionary{TKey,TValue}" />
    // ReSharper disable once UseCollectionCountProperty
    // We must use the HeadersEnumerator for this to work properly
    public int Count => GetHeaderEnumeration().Count();

    public bool IsReadOnly => false;

    /// <inheritdoc cref="IDictionary{TKey,TValue}" />
    public bool ContainsKey(string key)
    {
        return this._ContainsKey(key);
    }

    /// <inheritdoc />
    public void Add(string key, string? value)
    {
        this._Set(key, value);
    }

    /// <inheritdoc />
    public bool Remove(string key)
    {
        return this._Remove(key);
    }

    /// <inheritdoc cref="IDictionary{TKey,TValue}" />
    public bool TryGetValue(string key, out string? value)
    {
        return this._TryGetValue(key, out value);
    }

    /// <inheritdoc />
    public ICollection<string> Keys => this._Keys();

    /// <inheritdoc />
    public ICollection<string?> Values => this._Values();

    private IEnumerable<KeyValuePair<string, string?>> GetHeaderEnumeration()
    {
        if (Accept is not null)
            yield return new KeyValuePair<string, string?>(nameof(Accept), Accept.ToString());
        if (ContentType is not null)
            yield return new KeyValuePair<string, string?>("Content-Type", ContentType.ToString());
        if (Source is not null)
            yield return new KeyValuePair<string, string?>(nameof(Source), Source);
        if (Destination is not null)
            yield return new KeyValuePair<string, string?>(nameof(Destination), Destination);
        if (Authorization is not null)
            yield return new KeyValuePair<string, string?>(nameof(Authorization), "*******");
        if (Origin is not null)
            yield return new KeyValuePair<string, string?>(nameof(Origin), Origin);
        if (Elapsed is not null)
            yield return new KeyValuePair<string, string?>("RESTable-elapsed-ms", Elapsed.Value.ToStringRESTable());
        foreach (var (key, value) in _dict)
            yield return new KeyValuePair<string, string?>(key, value);
    }

    internal Headers GetCopy()
    {
        return new(this);
    }

    public readonly struct Enumerator : IDictionaryEnumerator
    {
        private IEnumerator<KeyValuePair<string, string?>> Inner { get; }

        public Enumerator(IEnumerator<KeyValuePair<string, string?>> inner) : this()
        {
            Inner = inner;
        }

        public bool MoveNext()
        {
            return Inner.MoveNext();
        }

        public void Reset()
        {
            Inner.Reset();
        }

        public object Current => Inner.Current;
        public DictionaryEntry Entry => new(Key, Value);
        public object Key => Inner.Current.Key;
        public object? Value => Inner.Current.Value;
    }

    #region Response headers

    #region Header names

    private const string _Info = "RESTable-info";
    private const string _Error = "RESTable-error";
    private const string _Metadata = "RESTable-metadata";
    private const string _Version = "RESTable-version";
    private const string _Vary = "Vary";
    private const string _AccessControlAllowOrigin = "Access-Control-Allow-Origin";
    private const string _AccessControlAllowMethods = "Access-Control-Allow-Methods";
    private const string _AccessControlMaxAge = "Access-Control-Max-Age";
    private const string _AccessControlAllowCredentials = "Access-Control-Allow-Credentials";
    private const string _AccessControlAllowHeaders = "Access-Control-Allow-Headers";

    #endregion

    public string? Info
    {
        get => this[_Info];
        set => this[_Info] = value;
    }

    public string? Error
    {
        get => this[_Error];
        set => this[_Error] = value;
    }

    public string? Metadata
    {
        get => this[_Metadata];
        set => this[_Metadata] = value;
    }

    public string? Version
    {
        get => this[_Version];
        set => this[_Version] = value;
    }

    public string? Vary
    {
        get => this[_Vary];
        set => this[_Vary] = value;
    }

    internal string? AccessControlAllowOrigin
    {
        get => this[_AccessControlAllowOrigin];
        set => this[_AccessControlAllowOrigin] = value;
    }

    internal string? AccessControlAllowMethods
    {
        get => this[_AccessControlAllowMethods];
        set => this[_AccessControlAllowMethods] = value;
    }

    internal string? AccessControlMaxAge
    {
        get => this[_AccessControlMaxAge];
        set => this[_AccessControlMaxAge] = value;
    }

    internal string? AccessControlAllowCredentials
    {
        get => this[_AccessControlAllowCredentials];
        set => this[_AccessControlAllowCredentials] = value;
    }

    internal string? AccessControlAllowHeaders
    {
        get => this[_AccessControlAllowHeaders];
        set => this[_AccessControlAllowHeaders] = value;
    }

    #endregion

    #region IHeadersInternal

    private void SetCustomHeader(string key, string? value)
    {
        _dict[key] = value;
    }

    bool IHeadersInternal.TryGetCustomHeader(string key, out string? value)
    {
        return _dict.TryGetValue(key, out value);
    }

    void IHeadersInternal.SetCustomHeader(string key, string? value)
    {
        SetCustomHeader(key, value);
    }

    bool IHeadersInternal.ContainsCustomHeader(KeyValuePair<string, string?> item)
    {
        return _dict.Contains(item);
    }

    bool IHeadersInternal.ContainsCustomHeaderName(string name)
    {
        return _dict.ContainsKey(name);
    }

    bool IHeadersInternal.RemoveCustomHeader(string name)
    {
        return _dict.Remove(name);
    }

    bool IHeadersInternal.RemoveCustomHeader(KeyValuePair<string, string?> header)
    {
        return _dict.Remove(header);
    }

    IEnumerable<KeyValuePair<string, string?>> IHeadersInternal.GetCustomHeaders()
    {
        return _dict;
    }

    #endregion
}
