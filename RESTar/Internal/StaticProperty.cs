using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Starcounter;

namespace RESTar.Internal
{
    public class StaticProperty : Property
    {
        public override string Name => PropertyInfo?.GetDataMemberNameOrName() ?? _name;
        public override string DatabaseQueryName => PropertyInfo?.Name ?? _databaseQueryName;
        public Type Type => PropertyInfo?.PropertyType ?? _type;
        public override bool Dynamic => false;
        public override bool IsStarcounterQueryable => PropertyInfo?.DeclaringType?.HasAttribute<DatabaseAttribute>() ?? false;

        internal bool IsObjectNo;
        internal bool IsObjectID;

        internal string _name;
        internal string _databaseQueryName;
        private Type _type;
        internal PropertyInfo PropertyInfo { get; private set; }

        public StaticProperty(PropertyInfo property)
        {
            if (property == null) return;
            PropertyInfo = property;
        }

        private StaticProperty()
        {
        }

        public static StaticProperty Parse(string keyString, Type resource)
        {
            var propertyInfo = resource.FindProperty(keyString);
            if (propertyInfo == null)
                throw new UnknownColumnException(resource, keyString);
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
            var propertyInfo = RESTarConfig.GetPropertyList(parentType)
                .FirstOrDefault(prop => prop.GetDataMemberNameOrName() == PropertyInfo.GetDataMemberNameOrName());
            if (propertyInfo == null)
                throw new UnknownColumnException(type, PropertyInfo.GetDataMemberNameOrName());
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
                if (IsObjectNo)
                    throw new Exception("Could not get ObjectNo from non-Starcounter resource.");
                if (IsObjectID)
                    throw new Exception("Could not get ObjectID from non-Starcounter resource.");
                throw;
            }
        }
    }
}