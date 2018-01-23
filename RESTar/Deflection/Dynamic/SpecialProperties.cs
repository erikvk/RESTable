using System;
using System.Collections.Generic;
using Newtonsoft.Json.Serialization;
using RESTar.Operations;
using Starcounter;
using static Newtonsoft.Json.NullValueHandling;
using static Newtonsoft.Json.ObjectCreationHandling;

namespace RESTar.Deflection.Dynamic
{
    /// <inheritdoc />
    /// <summary>
    /// Special properties are properties that are not strictly members, but still
    /// important parts of class definitions. For example Starcounter ObjectID and 
    /// ObjectNo.
    /// </summary>
    internal class SpecialProperty : DeclaredProperty
    {
        private class SpecialPropertyValueProvider : IValueProvider
        {
            private readonly DeclaredProperty Property;
            internal SpecialPropertyValueProvider(DeclaredProperty property) => Property = property;
            public void SetValue(object target, object value) => Property.Setter(target, value);
            public object GetValue(object target) => Property.Getter(target);
        }

        internal JsonProperty JsonProperty => new JsonProperty
        {
            PropertyType = Type,
            PropertyName = Name,
            Readable = Readable,
            Writable = Writable,
            ValueProvider = new SpecialPropertyValueProvider(this),
            ObjectCreationHandling = ReplaceOnUpdate ? Replace : Reuse,
            NullValueHandling = HiddenIfNull ? Ignore : Include,
            Order = Order
        };

        private SpecialProperty(int metadataToken, string name, string actualName, Type type, int? order, bool isKey, bool scQueryable,
            bool hidden, bool hiddenIfNull, Getter getter) : base
            (
                metadataToken: metadataToken,
                name: name,
                actualName: actualName,
                type: type,
                order: order,
                isKey: isKey,
                scQueryable: scQueryable,
                attributes: null,
                skipConditions: false,
                hidden: hidden,
                hiddenIfNull: hiddenIfNull,
                markedAsPrimitive: false,
                isEnum: false,
                allowedConditionOperators: Operators.All,
                getter: getter,
                setter: null
            ) { }

        internal static IEnumerable<SpecialProperty> GetObjectNoAndObjectID(bool flag) =>
            flag ? new[] {FlaggedObjectNo, FlaggedObjectID} : new[] {ObjectNo, ObjectID};

        // ReSharper disable PossibleNullReferenceException

        private static readonly int ObjectNoMetadataToken =
            typeof(DbHelper).GetMethod(nameof(DbHelper.GetObjectNo), new[] {typeof(object)}).MetadataToken;

        private static readonly int ObjectIDMetadataToken =
            typeof(DbHelper).GetMethod(nameof(DbHelper.GetObjectID), new[] {typeof(object)}).MetadataToken;

        // ReSharper restore PossibleNullReferenceException

        /// <summary>
        /// A property describing the ObjectNo of a class
        /// </summary>
        private static readonly SpecialProperty ObjectNo = new SpecialProperty
        (
            metadataToken: ObjectNoMetadataToken,
            name: "ObjectNo",
            actualName: "ObjectNo",
            type: typeof(ulong),
            order: int.MaxValue - 1,
            isKey: true,
            scQueryable: true,
            hidden: false,
            hiddenIfNull: false,
            getter: t => Do.TryAndThrow(t.GetObjectNo, "Could not get ObjectNo from non-Starcounter resource.")
        );

        /// <summary>
        /// A property describing the ObjectNo of a class
        /// </summary>
        private static readonly SpecialProperty FlaggedObjectNo = new SpecialProperty
        (
            metadataToken: ObjectNoMetadataToken,
            name: "$ObjectNo",
            actualName: "ObjectNo",
            type: typeof(ulong),
            order: int.MaxValue - 1,
            isKey: true,
            scQueryable: true,
            hidden: false,
            hiddenIfNull: false,
            getter: t => Do.TryAndThrow(t.GetObjectNo, "Could not get ObjectNo from non-Starcounter resource.")
        );

        /// <summary>
        /// A property describing the ObjectNo of a class
        /// </summary>
        private static readonly SpecialProperty ObjectID = new SpecialProperty
        (
            metadataToken: ObjectIDMetadataToken,
            name: "ObjectID",
            actualName: "ObjectID",
            type: typeof(string),
            order: int.MaxValue,
            isKey: false,
            scQueryable: true,
            hidden: true,
            hiddenIfNull: false,
            getter: t => Do.TryAndThrow(t.GetObjectID, "Could not get ObjectID from non-Starcounter resource.")
        );

        /// <summary>
        /// A property describing the ObjectNo of a class
        /// </summary>
        private static readonly SpecialProperty FlaggedObjectID = new SpecialProperty
        (
            metadataToken: ObjectIDMetadataToken,
            name: "$ObjectID",
            actualName: "ObjectID",
            type: typeof(string),
            order: int.MaxValue,
            isKey: false,
            scQueryable: true,
            hidden: true,
            hiddenIfNull: false,
            getter: t => Do.TryAndThrow(t.GetObjectID, "Could not get ObjectID from non-Starcounter resource.")
        );
    }
}