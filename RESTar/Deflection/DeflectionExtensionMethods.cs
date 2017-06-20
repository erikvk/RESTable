using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace RESTar.Deflection
{
    public static class DeflectionExtensionMethods
    {
        public static Getter MakeDynamicGetter(this PropertyInfo p)
        {
            try
            {
                if (p.DeclaringType?.IsValueType == true)
                    return p.GetValue;
                var getterDelegate = p
                    .GetGetMethod()?
                    .CreateDelegate(typeof(Func<,>)
                        .MakeGenericType(p.DeclaringType, p.PropertyType));
                return getterDelegate != null ? obj => ((dynamic) getterDelegate)((dynamic) obj) : default(Getter);
            }
            catch
            {
                return null;
            }
        }

        public static Setter MakeDynamicSetter(this PropertyInfo p)
        {
            try
            {
                if (p.DeclaringType?.IsValueType == true)
                    return p.SetValue;
                var setterDelegate = p
                    .GetSetMethod()?
                    .CreateDelegate(typeof(Action<,>)
                        .MakeGenericType(p.DeclaringType, p.PropertyType));
                return setterDelegate != null
                    ? (obj, value) => ((dynamic) setterDelegate)((dynamic) obj, value)
                    : default(Setter);
            }
            catch
            {
                return null;
            }
        }

        internal static StaticProperty MatchProperty(this Type resource, string str, bool ignoreCase = true)
        {
            var matches = resource.GetStaticProperties()
                .Where(p => string.Equals(str, p.Name, ignoreCase
                    ? StringComparison.CurrentCultureIgnoreCase
                    : StringComparison.CurrentCulture));
            var count = matches.Count();
            if (count == 0) throw new UnknownColumnException(resource, str);
            if (count > 1)
            {
                if (!ignoreCase)
                    throw new AmbiguousColumnException(resource, str, matches.Select(m => m.Name));
                return MatchProperty(resource, str, false);
            }
            return matches.First();
        }

        public static List<EnumMember> GetEnumMembers(this Type type) => type.IsEnum
            ? type.GetFields()
                .Where(t => t.FieldType.IsEnum)
                .Select(t => new EnumMember
                {
                    Attributes = t.GetCustomAttributes<Attribute>(),
                    Name = t.Name,
                    Value = (int) (Convert.ChangeType(t.GetValue(null), TypeCode.Int32) ?? -1)
                })
                .ToList()
            : throw new ArgumentException("Must be enum", nameof(type));

        public struct EnumMember
        {
            public IEnumerable<Attribute> Attributes;
            public string Name;
            public int Value;
            public bool HasAttribute<T>() where T : Attribute => Attributes.OfType<T>().Any();
            public T GetAttribute<T>() where T : Attribute => Attributes.OfType<T>().FirstOrDefault();
        }
    }
}