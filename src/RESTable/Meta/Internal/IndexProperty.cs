﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RESTable.Requests;

namespace RESTable.Meta.Internal
{
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
            metadataToken: index.GetHashCode(),
            name: name,
            actualName: name,
            type: type,
            order: null,
            attributes: new Attribute[0],
            skipConditions: false,
            hidden: true,
            hiddenIfNull: false,
            isEnum: type.IsEnum,
            customDateTimeFormat: null,
            allowedConditionOperators: Operators.All,
            readOnly: false,
            owner: owner,
            getter: target =>
            {
                try
                {
                    switch (target)
                    {
                        case IEnumerable<object> ie: return ie.ElementAtOrDefault(index);
                        case string str:
                            var length = str.Length;
                            return index >= length - 1 ? default : str[index];
                        case IList l:
                            var count = l.Count;
                            return index >= count - 1 ? null : l[index];
                        case IEnumerable e: return e.Cast<object>().ElementAtOrDefault(index);
                    }
                }
                catch { }
                return null;
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
                }),
            mergeOntoOwner: false
        ) { }
    }
}