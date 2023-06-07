using System;
using System.Collections;
using System.Collections.Generic;

namespace RESTable.Requests.Processors;

public sealed class ProcessedEntity : Dictionary<string, object?>
{
    public ProcessedEntity() : base(StringComparer.OrdinalIgnoreCase) { }
    public ProcessedEntity(int capacity) : base(capacity, StringComparer.OrdinalIgnoreCase) { }
    public ProcessedEntity(IDictionary<string, object?> dictionary) : base(dictionary, StringComparer.OrdinalIgnoreCase) { }
    public ProcessedEntity(IEnumerable<KeyValuePair<string, object?>> dictionary) : base(dictionary, StringComparer.OrdinalIgnoreCase) { }

    public ProcessedEntity(IDictionary dictionary) : base(dictionary.Count, StringComparer.OrdinalIgnoreCase)
    {
        foreach (DictionaryEntry entry in dictionary) Add(entry.Key.ToString()!, entry.Value);
    }
}
