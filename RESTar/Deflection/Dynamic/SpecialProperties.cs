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
    public class SpecialProperty : StaticProperty
    {
        private SpecialProperty(bool scQueryable) : base(scQueryable) { }

        internal static IEnumerable<SpecialProperty> GetObjectIDAndObjectNo(bool flag) =>
            flag ? new[] {FlaggedObjectID, FlaggedObjectNo} : new[] {ObjectID, ObjectNo};

        /// <summary>
        /// A property describing the ObjectNo of a class
        /// </summary>
        public static SpecialProperty FlaggedObjectNo => new SpecialProperty(true)
        {
            Name = "$ObjectNo",
            DatabaseQueryName = "ObjectNo",
            Type = typeof(ulong),
            Getter = t => Do.TryAndThrow(t.GetObjectNo, "Could not get ObjectNo from non-Starcounter resource."),
            Hidden = true,
            Order = int.MaxValue - 1
        };

        /// <summary>
        /// A property describing the ObjectNo of a class
        /// </summary>
        public static SpecialProperty FlaggedObjectID => new SpecialProperty(true)
        {
            Name = "$ObjectID",
            DatabaseQueryName = "ObjectID",
            Type = typeof(string),
            Getter = t => Do.TryAndThrow(t.GetObjectID, "Could not get ObjectID from non-Starcounter resource."),
            Hidden = true,
            Order = int.MaxValue
        };

        /// <summary>
        /// A property describing the ObjectNo of a class
        /// </summary>
        public static SpecialProperty ObjectNo => new SpecialProperty(true)
        {
            Name = "ObjectNo",
            DatabaseQueryName = "ObjectNo",
            Type = typeof(ulong),
            Getter = t => Do.TryAndThrow(t.GetObjectNo, "Could not get ObjectNo from non-Starcounter resource."),
            Hidden = true,
            Order = int.MaxValue - 1
        };

        /// <summary>
        /// A property describing the ObjectNo of a class
        /// </summary>
        public static SpecialProperty ObjectID => new SpecialProperty(true)
        {
            Name = "ObjectID",
            DatabaseQueryName = "ObjectID",
            Type = typeof(string),
            Getter = t => Do.TryAndThrow(t.GetObjectID, "Could not get ObjectID from non-Starcounter resource."),
            Hidden = true,
            Order = int.MaxValue
        };
    }
}