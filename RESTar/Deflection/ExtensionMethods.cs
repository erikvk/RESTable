﻿using System;
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
                if (p.DeclaringType?.IsValueType == true)
                    return p.GetValue;
                if (p.GetIndexParameters().Any()) return null;
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
                if (p.GetIndexParameters().Any()) return null;
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
    }
}