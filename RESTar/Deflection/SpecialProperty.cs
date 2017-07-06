using Newtonsoft.Json;
using Starcounter;
using static RESTar.Operations.Do;

namespace RESTar.Deflection
{
    /// <summary>
    /// Special properties are properties that are not strictly members, but still
    /// important parts of class definitions. For example Starcounter ObjectID and 
    /// ObjectNo.
    /// </summary>
    public class SpecialProperty : StaticProperty
    {
        private SpecialProperty(bool scQueryable) : base(scQueryable)
        {
        }

        /// <summary>
        /// A property describing the ObjectNo of a class
        /// </summary>
        public static SpecialProperty ObjectNo => new SpecialProperty(true)
        {
            Name = "ObjectNo",
            DatabaseQueryName = "ObjectNo",
            Type = typeof(ulong),
            Getter = t => TryAndThrow(() => t.GetObjectNo(), "Could not get ObjectNo from non-Starcounter resource."),
            Attributes = new[] {new JsonPropertyAttribute {Order = int.MaxValue - 1}}
        };

        /// <summary>
        /// A property describing the ObjectNo of a class
        /// </summary>
        public static SpecialProperty ObjectID => new SpecialProperty(true)
        {
            Name = "ObjectID",
            DatabaseQueryName = "ObjectID",
            Type = typeof(string),
            Getter = t => TryAndThrow(() => t.GetObjectID(), "Could not get ObjectID from non-Starcounter resource."),
            Attributes = new[] {new JsonPropertyAttribute {Order = int.MaxValue}}
        };
    }
}