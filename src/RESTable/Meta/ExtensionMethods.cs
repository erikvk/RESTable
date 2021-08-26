using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using RESTable.Resources;

namespace RESTable.Meta
{
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
            var valueProvider = new ExpressionValueProvider(propertyInfo);
            return target => new ValueTask<object?>(valueProvider.GetValue(target));
        }

        /// <summary>
        /// Makes a fast delegate for setting the value for a given property.
        /// </summary>
        public static Setter MakeDynamicSetter(this PropertyInfo propertyInfo)
        {
            var valueProvider = new ExpressionValueProvider(propertyInfo);
            return (target, value) =>
            {
                valueProvider.SetValue(target, value);
                return default;
            };
        }

        /// <summary>
        /// Makes a fast delegate for creating an instance of a given type
        /// </summary>
        public static Constructor? MakeDynamicConstructor(this Type type)
        {
            try
            {
                return type switch
                {
                    _ when type.GetConstructor(Type.EmptyTypes) is null => null,
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
        public static Constructor<T>? MakeStaticConstructor<T>(this Type type)
        {
            try
            {
                return type switch
                {
                    _ when type.GetConstructor(Type.EmptyTypes) is null => null,
                    _ => Expression.Lambda<Constructor<T>>(Expression.New(type)).Compile()
                };
            }
            catch
            {
                return null;
            }
        }

        internal static bool IsImplemented(this MethodInfo method) => !method
            .GetCustomAttributes(false)
            .OfType<MethodNotImplementedAttribute>()
            .Any();
    }
}