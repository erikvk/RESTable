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
            string customDateTimeFormat = null,
            Operators allowedConditionOperators = Operators.All
        ) : base
        (
            $"{owner.FullName}.{name}".GetHashCode(),
            name,
            actualName ?? name,
            type,
            order,
            attributes,
            skipConditions,
            hidden,
            hiddenIfNull,
            isEnum,
            customDateTimeFormat,
            allowedConditionOperators,
            owner,
            getter,
            setter
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
            string customDateTimeFormat = null,
            Operators allowedConditionOperators = Operators.All
        ) : base
        (
            $"{typeof(TOwner).FullName}.{name}".GetHashCode(),
            name,
            actualName ?? name,
            typeof(TPropertyType),
            order,
            attributes,
            skipConditions,
            hidden,
            hiddenIfNull,
            isEnum,
            customDateTimeFormat,
            allowedConditionOperators,
            typeof(TOwner),
            getter == null ? default(Getter) : o => getter((TOwner) o),
            setter == null ? default(Setter) : (o, v) => setter((TOwner) o, (TPropertyType) v)
        ) { }
    }
}