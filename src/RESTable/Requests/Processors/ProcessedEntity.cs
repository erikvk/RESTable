using System;
using System.Collections.Generic;

namespace RESTable.Requests.Processors
{
    public sealed class ProcessedEntity : Dictionary<string, object?>
    {
        public ProcessedEntity() : base(StringComparer.OrdinalIgnoreCase) { }
        public ProcessedEntity(int capacity) : base(capacity, StringComparer.OrdinalIgnoreCase) { }
        public ProcessedEntity(IDictionary<string, object?> dictionary) : base(dictionary, StringComparer.OrdinalIgnoreCase) { }
    }
}