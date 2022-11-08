using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using RESTable.Meta;

namespace RESTable.Sqlite;

/// <inheritdoc cref="IDictionary{TKey,TValue}" />
/// <inheritdoc cref="IDynamicMemberValueProvider" />
/// <summary>
///     Defines the dynamic members of an elastic Sqlite table
/// </summary>
public class DynamicMemberCollection : IDictionary<string, object?>, IDynamicMemberValueProvider
{
    public DynamicMemberCollection(TableMapping tableMapping)
    {
        TableMapping = tableMapping;
        ValueDictionary = new ConcurrentDictionary<string, KeyValuePair<string, object?>>(StringComparer.OrdinalIgnoreCase);
    }

    private IDictionary<string, KeyValuePair<string, object?>> ValueDictionary { get; }
    private TableMapping TableMapping { get; }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
    {
        return ValueDictionary.Values.GetEnumerator();
    }

    /// <inheritdoc />
    public bool ContainsKey(string key)
    {
        return ValueDictionary.ContainsKey(key);
    }

    /// <inheritdoc />
    public void Add(string key, object? value)
    {
        ValueDictionary.Add(key, new KeyValuePair<string, object?>(key, value));
    }

    /// <inheritdoc />
    public bool Remove(string key)
    {
        return ValueDictionary.Remove(key);
    }

    /// <inheritdoc />
    public bool TryGetValue(string key, out object? value)
    {
        return TryGetValue(key, out value, out _);
    }

    /// <inheritdoc />
    public ICollection<string> Keys => ValueDictionary.Keys;

    /// <inheritdoc />
    public ICollection<object?> Values => ValueDictionary.Values.Select(item => item.Value).ToList();

    /// <inheritdoc />
    public void Add(KeyValuePair<string, object?> item)
    {
        ValueDictionary.Add(item.Key, item);
    }

    /// <inheritdoc />
    public void Clear()
    {
        ValueDictionary.Clear();
    }

    /// <inheritdoc />
    public void CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex)
    {
        if (array is null) throw new ArgumentNullException(nameof(array));
        if (arrayIndex < 0) throw new ArgumentOutOfRangeException(nameof(arrayIndex));
        if (arrayIndex + Count > array.Length - 1) throw new ArgumentException("Invalid arrayIndex", nameof(arrayIndex));
        foreach (var (key, value) in ValueDictionary)
        {
            array[arrayIndex] = new KeyValuePair<string, object?>(key, value.Value);
            arrayIndex += 1;
        }
    }

    /// <inheritdoc />
    public bool Remove(KeyValuePair<string, object?> item)
    {
        return ValueDictionary.Remove(new KeyValuePair<string, KeyValuePair<string, object?>>(item.Key, item));
    }

    /// <inheritdoc />
    public int Count => ValueDictionary.Count;

    /// <inheritdoc />
    public bool IsReadOnly => ValueDictionary.IsReadOnly;

    /// <inheritdoc />
    public object? this[string key]
    {
        get => ValueDictionary[key].Value;
        set => TrySetValue(key, value);
    }

    /// <inheritdoc />
    public bool Contains(KeyValuePair<string, object?> item)
    {
        return ValueDictionary.Contains(new KeyValuePair<string, KeyValuePair<string, object?>>(item.Key, item));
    }

    /// <inheritdoc />
    public bool TryGetValue(string memberName, out object? value, out string? actualMemberName)
    {
        if (ValueDictionary.TryGetValue(memberName, out var pair))
        {
            value = pair.Value;
            actualMemberName = pair.Key;
            return true;
        }
        value = actualMemberName = null;
        return false;
    }

    /// <inheritdoc />
    public bool TrySetValue(string memberName, object? value)
    {
        if (TryGetValue(memberName, out _, out var actualMemberName))
            memberName = actualMemberName!;
        else if (TableMapping.ColumnMappings.TryGetValue(memberName, out var match))
            memberName = match.ClrProperty.Name;
        ValueDictionary[memberName] = new KeyValuePair<string, object?>(memberName, value);
        return true;
    }

    /// <summary>
    ///     Returns the value with the given member name, or null if there is no such value
    /// </summary>
    public object? SafeGet(string memberName)
    {
        ValueDictionary.TryGetValue(memberName, out var pair);
        return pair.Value;
    }
}
