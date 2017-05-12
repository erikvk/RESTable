using System;
using System.Reflection;
using Starcounter;

namespace RESTar.Internal
{
    public class StaticProperty : Property
    {
        public override string Name => PropertyInfo?.RESTarMemberName() ?? _name;
        public override string DatabaseQueryName => PropertyInfo?.Name ?? _databaseQueryName;
        public Type Type => PropertyInfo?.PropertyType ?? _type;
        public override bool Dynamic => false;
        internal bool IsObjectNo;
        internal bool IsObjectID;
        private string _name;
        private string _databaseQueryName;
        private Type _type;
        private PropertyInfo PropertyInfo { get; set; }

        public override bool ScQueryable => PropertyInfo?.DeclaringType?.HasAttribute<DatabaseAttribute>() == true &&
                                            PropertyInfo.PropertyType.IsStarcounterCompatible();

        private StaticProperty(PropertyInfo property)
        {
            if (property == null) return;
            PropertyInfo = property;
        }

        private StaticProperty()
        {
        }

        public static StaticProperty Parse(string keyString, Type resource)
        {
            var propertyInfo = resource.MatchProperty(keyString);
            return new StaticProperty(propertyInfo);
        }

        public static StaticProperty ObjectNo => new StaticProperty
        {
            _name = "ObjectNo",
            _databaseQueryName = "ObjectNo",
            _type = typeof(ulong),
            IsObjectNo = true
        };

        public static StaticProperty ObjectID => new StaticProperty
        {
            _name = "ObjectID",
            _databaseQueryName = "ObjectID",
            _type = typeof(string),
            IsObjectID = true
        };

        internal void Migrate(Type type, StaticProperty previous)
        {
            if (IsObjectID || IsObjectNo) return;
            var parentType = previous?.Type ?? type;
            var propertyInfo = parentType.MatchProperty(PropertyInfo.RESTarMemberName());
            if (propertyInfo == null)
                throw new UnknownColumnException(type, PropertyInfo.RESTarMemberName());
            PropertyInfo = propertyInfo;
        }

        internal override dynamic GetValue(dynamic input)
        {
            try
            {
                if (IsObjectNo) return DbHelper.GetObjectNo(input);
                if (IsObjectID) return DbHelper.GetObjectID(input);
                return PropertyInfo.GetValue(input);
            }
            catch (InvalidCastException)
            {
                if (IsObjectNo) throw new Exception("Could not get ObjectNo from non-Starcounter resource.");
                if (IsObjectID) throw new Exception("Could not get ObjectID from non-Starcounter resource.");
                throw;
            }
        }
    }
}