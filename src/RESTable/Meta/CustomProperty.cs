using System;
using System.Collections.Generic;
using RESTable.Requests;

namespace RESTable.Meta
{
    public class CustomProperty : DeclaredProperty
    {
        public CustomProperty
        (
            string name,
            Type owner,
            Type type,
            Getter? getter = null,
            Setter? setter = null,
            string? actualName = null,
            int? order = null,
            ICollection<Attribute>? attributes = null,
            bool skipConditions = false,
            bool hidden = false,
            bool hiddenIfNull = false,
            bool mergeOntoOwner = false,
            bool readOnly = false,
            Operators allowedConditionOperators = Operators.All
        ) : base
        (
            metadataToken: $"{owner.FullName}.{name}".GetHashCode(),
            name: name,
            actualName: actualName ?? name,
            type: type,
            order: order,
            attributes: attributes ?? Array.Empty<Attribute>(),
            skipConditions: skipConditions,
            hidden: hidden,
            hiddenIfNull: hiddenIfNull,
            allowedConditionOperators: allowedConditionOperators,
            owner: owner,
            getter: getter,
            setter: setter,
            mergeOntoOwner: mergeOntoOwner,
            excelReducer: null,
            readOnly: readOnly
        ) { }
    }

    public class CustomProperty<TOwner, TPropertyType> : DeclaredProperty
    {
        public CustomProperty
        (
            string name,
            Getter<TOwner, TPropertyType>? getter = null,
            Setter<TOwner, TPropertyType>? setter = null,
            string? actualName = null,
            int? order = null,
            ICollection<Attribute>? attributes = null,
            bool skipConditions = false,
            bool hidden = false,
            bool hiddenIfNull = false,
            bool mergeOntoOwner = false,
            bool readOnly = false,
            Operators allowedConditionOperators = Operators.All
        ) : base
        (
            metadataToken: $"{typeof(TOwner).FullName}.{name}".GetHashCode(),
            name: name,
            actualName: actualName ?? name,
            type: typeof(TPropertyType),
            order: order,
            attributes: attributes ?? Array.Empty<Attribute>(),
            skipConditions: skipConditions,
            hidden: hidden,
            hiddenIfNull: hiddenIfNull,
            allowedConditionOperators: allowedConditionOperators,
            owner: typeof(TOwner),
            excelReducer: null,
            getter: getter is null
                ? default(Getter)
                : async o =>
                {
                    var value = await getter((TOwner) o).ConfigureAwait(false);
                    return value;
                },
            setter: setter is null
                ? default(Setter)
                : async (o, v) =>
                {
                    await setter((TOwner) o, (TPropertyType) v!).ConfigureAwait(false);
                },
            mergeOntoOwner: mergeOntoOwner,
            readOnly: readOnly
        ) { }
    }
}