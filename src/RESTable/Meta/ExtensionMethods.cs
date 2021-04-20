using System;
using System.Linq.Expressions;
using System.Reflection;
using Newtonsoft.Json.Serialization;
using RESTable.ContentTypeProviders;

namespace RESTable.Meta
{
    internal static class ApplicationServicesAccessor
    {
        internal static IJsonProvider JsonProvider { get; set; }
        internal static TypeCache TypeCache { get; set; }
        internal static ResourceCollection ResourceCollection { get; set; }
    }

    /// <summary>
    /// Extension methods for deflection operations
    /// </summary>
    public static class ExtensionMethods
    {
        /// <summary>
        /// Makes a fast delegate for getting the value for a given property.
        /// </summary>
        public static Getter MakeDynamicGetter(this PropertyInfo propertyInfo)
        {
            try
            {
                var valueProvider = new ExpressionValueProvider(propertyInfo);
                return target => valueProvider.GetValue(target);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Makes a fast delegate for setting the value for a given property.
        /// </summary>
        public static Setter MakeDynamicSetter(this PropertyInfo propertyInfo)
        {
            try
            {
                var valueProvider = new ExpressionValueProvider(propertyInfo);
                return (target, value) => valueProvider.SetValue(target, value);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Makes a fast delegate for setting the value for a given property.
        /// </summary>
        public static Constructor MakeDynamicConstructor(this Type type)
        {
            try
            {
                return type switch
                {
                    _ when type.GetConstructor(Type.EmptyTypes) == null => null,
                    _ => Expression.Lambda<Constructor>(Expression.New(type)).Compile()
                };
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Makes a fast delegate for setting the value for a given property.
        /// </summary>
        public static Constructor<T> MakeStaticConstructor<T>(this Type type)
        {
            try
            {
                return type switch
                {
                    _ when type.GetConstructor(Type.EmptyTypes) == null => null,
                    _ => Expression.Lambda<Constructor<T>>(Expression.New(type)).Compile()
                };
            }
            catch
            {
                return null;
            }
        }
    }
}