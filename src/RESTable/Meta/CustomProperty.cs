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
            Getter getter = null,
            Setter setter = null,
            string actualName = null,
            int? order = null,
            ICollection<Attribute> attributes = null,
            bool skipConditions = false,
            bool hidden = false,
            bool hiddenIfNull = false,
            bool isEnum = false,
            bool mergeOntoOwner = false,
            bool readOnly = false,
            string customDateTimeFormat = null,
            Operators allowedConditionOperators = Operators.All
        ) : base
        (
            metadataToken: $"{owner.FullName}.{name}".GetHashCode(),
            name: name,
            actualName: actualName ?? name,
            type: type,
            order: order,
            attributes: attributes,
            skipConditions: skipConditions,
            hidden: hidden,
            hiddenIfNull: hiddenIfNull,
            isEnum: isEnum,
            customDateTimeFormat: customDateTimeFormat,
            allowedConditionOperators: allowedConditionOperators,
            owner: owner,
            getter: getter,
            setter: setter,
            mergeOntoOwner: mergeOntoOwner,
            readOnly: readOnly
        ) { }
    }

    public class CustomProperty<TOwner, TPropertyType> : DeclaredProperty
    {
        public CustomProperty
        (
            string name,
            Getter<TOwner, TPropertyType> getter = null,
            Setter<TOwner, TPropertyType> setter = null,
            string actualName = null,
            int? order = null,
            ICollection<Attribute> attributes = null,
            bool skipConditions = false,
            bool hidden = false,
            bool hiddenIfNull = false,
            bool isEnum = false,
            bool mergeOntoOwner = false,
            bool readOnly = false,
            string customDateTimeFormat = null,
            Operators allowedConditionOperators = Operators.All
        ) : base
        (
            metadataToken: $"{typeof(TOwner).FullName}.{name}".GetHashCode(),
            name: name,
            actualName: actualName ?? name,
            type: typeof(TPropertyType),
            order: order,
            attributes: attributes,
            skipConditions: skipConditions,
            hidden: hidden,
            hiddenIfNull: hiddenIfNull,
            isEnum: isEnum,
            customDateTimeFormat: customDateTimeFormat,
            allowedConditionOperators: allowedConditionOperators,
            owner: typeof(TOwner),
            getter: getter is null ? default(Getter) : o => getter((TOwner) o),
            setter: setter is null ? default(Setter) : (o, v) => setter((TOwner) o, (TPropertyType) v),
            mergeOntoOwner: mergeOntoOwner,
            readOnly: readOnly
        ) { }
    }
}