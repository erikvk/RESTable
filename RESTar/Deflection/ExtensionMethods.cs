using System;
using System.Reflection;
using RESTar.Deflection.Dynamic;

namespace RESTar.Deflection
{
    /// <summary>
    /// Extension methods for deflection operations
    /// </summary>
    public static class ExtensionMethods
    {
        /// <summary>
        /// Makes a fast delegate for getting the value for a given property.
        /// </summary>
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

        /// <summary>
        /// Makes a fast delegate for setting the value for a given property.
        /// </summary>
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

        /// <summary>
        /// Converts a PropertyInfo to a Term
        /// </summary>
        public static Term ToTerm(this PropertyInfo propertyInfo) => propertyInfo.DeclaringType
            .MakeTerm(propertyInfo.Name, Resource.Get(propertyInfo.DeclaringType)?.IsDynamic == true);
    }
}