﻿using System;
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
        public static Getter MakeDynamicGetter(this PropertyInfo p)
        {
            try
            {
                var valueProvider = new ExpressionValueProvider(p);
                return target => valueProvider.GetValue(target);

                // switch (p)
                // {
                //     case var _ when p.DeclaringType?.IsValueType == true: return p.GetValue;
                //     case var _ when p.GetIndexParameters().Any(): return null;
                //     default:
                //         var getter = p.GetGetMethod()?.CreateDelegate(typeof(Func<,>).MakeGenericType(p.DeclaringType, p.PropertyType));
                //         return getter != null
                //             ? obj =>
                //             {
                //                 try
                //                 {
                //                     return ((dynamic) getter)((dynamic) obj);
                //                 }
                //                 catch (RuntimeBinderException)
                //                 {
                //                     return p.GetValue(obj);
                //                 }
                //             }
                //             : default(Getter);
                // }
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
                var valueProvider = new ExpressionValueProvider(p);
                return (target, value) => valueProvider.SetValue(target, value);

//                switch (p)
//                {
//                    case var _ when p.DeclaringType?.IsValueType == true: return p.SetValue;
//                    case var _ when p.GetIndexParameters().Any(): return null;
//                    default:
//                        
//                        var setter = p.GetSetMethod()?.CreateDelegate(typeof(Action<,>).MakeGenericType(p.DeclaringType, p.PropertyType));
//                        return setter != null
//                            ? (obj, value) =>
//                            {
//                                try
//                                {
//                                    ((dynamic) setter)((dynamic) obj, (dynamic) value);
//                                }
//                                catch (RuntimeBinderException)
//                                {
//                                    p.SetValue(obj, value);
//                                }
//                            }
//                            : default(Setter);
//                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Makes a fast delegate for setting the value for a given property.
        /// </summary>
        public static Constructor MakeDynamicConstructor(this Type t)
        {
            try
            {
                switch (t)
                {
                    case var _ when t.GetConstructor(Type.EmptyTypes) == null: return null;
                    default: return Expression.Lambda<Constructor>(Expression.New(t)).Compile();
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Makes a fast delegate for setting the value for a given property.
        /// </summary>
        public static Constructor<T> MakeStaticConstructor<T>(this Type t)
        {
            try
            {
                switch (t)
                {
                    case var _ when t.GetConstructor(Type.EmptyTypes) == null: return null;
                    default: return Expression.Lambda<Constructor<T>>(Expression.New(t)).Compile();
                }
            }
            catch
            {
                return null;
            }
        }
    }
}