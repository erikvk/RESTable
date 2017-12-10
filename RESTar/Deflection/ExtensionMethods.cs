using System;
using System.Linq;
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
                switch (p)
                {
                    case var _ when p.DeclaringType?.IsValueType == true: return p.GetValue;
                    case var _ when p.GetIndexParameters().Any(): return null;
                }

                var getter = p.GetGetMethod()?.CreateDelegate(typeof(Func<,>).MakeGenericType(p.DeclaringType, p.PropertyType));
                return getter != null ? obj => ((dynamic) getter)((dynamic) obj) : default(Getter);
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
                switch (p)
                {
                    case var _ when p.DeclaringType?.IsValueType == true: return p.SetValue;
                    case var _ when p.GetIndexParameters().Any(): return null;
                }

                var setter = p.GetSetMethod()?.CreateDelegate(typeof(Action<,>).MakeGenericType(p.DeclaringType, p.PropertyType));
                return setter != null ? (obj, value) => ((dynamic) setter)((dynamic) obj, value) : default(Setter);
            }
            catch
            {
                return null;
            }
        }
    }
}