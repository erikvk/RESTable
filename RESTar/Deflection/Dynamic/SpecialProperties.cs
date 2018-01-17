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
    internal class SpecialProperty : DeclaredProperty
    {
        private SpecialProperty(int metadataToken, string name, string actualName, Type type, int? order, bool scQueryable,
            bool hidden, bool hiddenIfNull, Getter getter) : base
            (
                metadataToken: metadataToken,
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

        // ReSharper disable PossibleNullReferenceException

        private static readonly int ObjectNoMetadataToken =
            typeof(DbHelper).GetMethod(nameof(DbHelper.GetObjectNo), new[] {typeof(object)}).MetadataToken;

        private static readonly int ObjectIDMetadataToken =
            typeof(DbHelper).GetMethod(nameof(DbHelper.GetObjectID), new[] {typeof(object)}).MetadataToken;

        // ReSharper restore PossibleNullReferenceException

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
            metadataToken: ObjectIDMetadataToken,
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
            metadataToken: ObjectNoMetadataToken,
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
            metadataToken: ObjectIDMetadataToken,
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