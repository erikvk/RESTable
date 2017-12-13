using System;
using System.Collections.Generic;
using RESTar.Operations;
using Starcounter;

namespace RESTar.Deflection.Dynamic
{
    /// <inheritdoc />
    /// <summary>
    /// Special properties are properties that are not strictly members, but still
    /// important parts of class definitions. For example Starcounter ObjectID and 
    /// ObjectNo.
    /// </summary>
    internal class SpecialProperty : StaticProperty
    {
        private SpecialProperty(string name, string actualName, Type type, int? order, bool scQueryable,
            bool hidden, bool hiddenIfNull, Getter getter) : base
            (
                name: name,
                actualName: actualName,
                type: type,
                order: order,
                scQueryable: scQueryable,
                attributes: null,
                skipConditions: false,
                hidden: hidden,
                hiddenIfNull: hiddenIfNull,
                allowedConditionOperators: Operators.All,
                getter: getter,
                setter: null
            ) { }

        internal static IEnumerable<SpecialProperty> GetObjectIDAndObjectNo(bool flag) =>
            flag ? new[] {FlaggedObjectID, FlaggedObjectNo} : new[] {ObjectID, ObjectNo};

        /// <summary>
        /// A property describing the ObjectNo of a class
        /// </summary>
        private static readonly SpecialProperty FlaggedObjectNo = new SpecialProperty
        (
            name: "$ObjectNo",
            actualName: "ObjectNo",
            type: typeof(ulong),
            order: int.MaxValue - 1,
            scQueryable: true,
            hidden: true,
            hiddenIfNull: false,
            getter: t => Do.TryAndThrow(t.GetObjectNo, "Could not get ObjectNo from non-Starcounter resource.")
        );

        /// <summary>
        /// A property describing the ObjectNo of a class
        /// </summary>
        private static readonly SpecialProperty FlaggedObjectID = new SpecialProperty
        (
            name: "$ObjectID",
            actualName: "ObjectID",
            type: typeof(string),
            order: int.MaxValue,
            scQueryable: true,
            hidden: true,
            hiddenIfNull: false,
            getter: t => Do.TryAndThrow(t.GetObjectID, "Could not get ObjectID from non-Starcounter resource.")
        );

        /// <summary>
        /// A property describing the ObjectNo of a class
        /// </summary>
        private static readonly SpecialProperty ObjectNo = new SpecialProperty
        (
            name: "ObjectNo",
            actualName: "ObjectNo",
            type: typeof(ulong),
            order: int.MaxValue - 1,
            scQueryable: true,
            hidden: true,
            hiddenIfNull: false,
            getter: t => Do.TryAndThrow(t.GetObjectNo, "Could not get ObjectNo from non-Starcounter resource.")
        );


        /// <summary>
        /// A property describing the ObjectNo of a class
        /// </summary>
        private static readonly SpecialProperty ObjectID = new SpecialProperty
        (
            name: "ObjectID",
            actualName: "ObjectID",
            type: typeof(string),
            order: int.MaxValue,
            scQueryable: true,
            hidden: true,
            hiddenIfNull: false,
            getter: t => Do.TryAndThrow(t.GetObjectID, "Could not get ObjectID from non-Starcounter resource.")
        );
    }
}