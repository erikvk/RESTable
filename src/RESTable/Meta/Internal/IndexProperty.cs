using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RESTable.Requests;

namespace RESTable.Meta.Internal;

internal class IndexProperty : DeclaredProperty
{
    internal IndexProperty
    (
        int index,
        string name,
        Type type,
        bool collectionReadonly,
        Type owner
    ) : base
    (
        index.GetHashCode(),
        name,
        name,
        type,
        null,
        Array.Empty<Attribute>(),
        false,
        true,
        false,
        allowedConditionOperators: Operators.All,
        readOnly: false,
        excelReducer: null,
        owner: owner,
        getter: async target =>
        {
            switch (target)
            {
                case IAsyncEnumerable<object> ae: return await ae.ElementAtOrDefaultAsync(index).ConfigureAwait(false);
                case IEnumerable<object> ie: return ie.ElementAtOrDefault(index);
                case string str:
                    var length = str.Length;
                    return index >= length - 1 ? default : str[index];
                case IList l:
                    var count = l.Count;
                    return index >= count - 1 ? null : l[index];
                case IEnumerable e: return e.Cast<object>().ElementAtOrDefault(index);
                default: throw new Exception("Could not get value at index " + index);
            }
        },
        setter: collectionReadonly
            ? default
            : new Setter((target, value) =>
            {
                switch (target)
                {
                    case IList l:
                        try
                        {
                            l[index] = value;
                        }
                        catch { }
                        break;
                    default:
                        try
                        {
                            // we know that it is IList<T> of something (which does not make it IList!)
                            // so it should have an indexer
                            dynamic dynTarget = target;
                            dynTarget[index] = value;
                        }
                        catch { }
                        break;
                }
                return default;
            }),
        mergeOntoOwner: false
    ) { }
}
