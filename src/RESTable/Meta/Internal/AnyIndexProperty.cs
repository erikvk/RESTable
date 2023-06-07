using System;
using RESTable.Requests;

namespace RESTable.Meta.Internal;

/// <inheritdoc />
/// <summary>
///     Used to indicate a property that could refer to any index in an enumeration
/// </summary>
public class AnyIndexProperty : DeclaredProperty
{
    public AnyIndexProperty
    (
        Type type,
        Type owner
    ) : base
    (
        "*".GetHashCode(),
        "<any index>",
        "<any index>",
        type,
        null,
        Array.Empty<Attribute>(),
        false,
        true,
        false,
        allowedConditionOperators: Operators.All,
        owner: owner,
        getter: null,
        setter: null,
        mergeOntoOwner: false,
        readOnly: false,
        excelReducer: null
    ) { }
}
