using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RESTable.Requests;

namespace RESTable.Meta.Internal
{
    internal class LastIndexProperty : DeclaredProperty
    {
        internal LastIndexProperty(Type type, bool collectionReadonly, Type owner) : base
        (
            metadataToken: "-".GetHashCode(),
            name: "-",
            actualName: "-",
            type: type,
            order: null,
            attributes: Array.Empty<Attribute>(),
            skipConditions: false,
            hidden: true,
            hiddenIfNull: false,
            readOnly: false,
            allowedConditionOperators: Operators.All,
            owner: owner,
            excelReducer: null,
            getter: async target =>
            {
                switch (target)
                {
                    case IAsyncEnumerable<object> ae: return await ae.LastOrDefaultAsync().ConfigureAwait(false);
                    case IEnumerable<object> ie: return ie.LastOrDefault();
                    case string str: return str.Last();
                    case IList l:
                        var count = l.Count;
                        return count == 0 ? null : l[l.Count - 1];
                    case IEnumerable e: return e.Cast<object>().LastOrDefault();
                    default: throw new Exception("Could not get the last value of the enumeration");
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
                                var count = l.Count;
                                if (count == 0)
                                    l.Add(value);
                                else l[l.Count - 1] = value;
                            }
                            catch { }
                            break;
                        default:
                            try
                            {
                                // we know that it is IList<T> of something (which does not make it IList!)
                                // so it should have an indexer and a Count property.
                                dynamic l = target;
                                int count = l.Count;
                                if (count == 0)
                                    l.Add(value);
                                else l[l.Count - 1] = value;
                            }
                            catch { }
                            break;
                    }
                    return default;
                }),
            mergeOntoOwner: false
        ) { }
    }
}