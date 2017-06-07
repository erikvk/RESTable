using RESTar.Operations;
using Starcounter;

namespace RESTar.Deflection
{
    public class SpecialProperty : StaticProperty
    {
        private SpecialProperty(bool scQueryable) : base(scQueryable)
        {
        }

        public static SpecialProperty ObjectNo => new SpecialProperty(true)
        {
            Name = "ObjectNo",
            DatabaseQueryName = "ObjectNo",
            Type = typeof(ulong),
            Getter = t => Do.TryAndThrow(() => t.GetObjectNo(), "Could not get ObjectNo from non-Starcounter resource.")
        };

        public static SpecialProperty ObjectID => new SpecialProperty(true)
        {
            Name = "ObjectID",
            DatabaseQueryName = "ObjectID",
            Type = typeof(string),
            Getter = t => Do.TryAndThrow(() => t.GetObjectID(), "Could not get ObjectID from non-Starcounter resource.")
        };
    }
}