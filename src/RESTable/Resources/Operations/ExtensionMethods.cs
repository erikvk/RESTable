using System;
using System.Runtime.CompilerServices;
using RESTable.Meta;

namespace RESTable.Resources.Operations
{
    /// <summary>
    /// Extension methods for resource declarations
    /// </summary>
    public static class ExtensionMethods
    {
        /// <summary>
        /// Notifies listeners for property changes that a new change has taken place on a given property,
        /// provided by the compiler (from the context of the call to NotifyChange) Place the call to this
        /// method inside the set accessor of the property. Include the old value. This should be called
        /// AFTER the change has been pushed to the target object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="target">The target object that has had a property changed</param>
        /// <param name="propertyName">The name of the changed property, supplied by the compiler
        /// (if called from the property set accessor)</param>
        public static void NotifyState<T>(this T target, [CallerMemberName] string? propertyName = null)
            where T : class, IPropertyChangeNotifier
        {
            NotifyState(target, default(UnknownValue), propertyName);
        }

        /// <summary>
        /// Notifies listeners for property changes that a new change has taken place on a given property,
        /// provided by the compiler (from the context of the call to NotifyChange) Place the call to this
        /// method inside the set accessor of the property. Include the old value. This should be called
        /// AFTER the change has been pushed to the target object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="target">The target object that has had a property changed</param>
        /// <param name="oldValue">The old value of the changed property</param>
        /// <param name="propertyName">The name of the changed property, supplied by the compiler
        /// (if called from the property set accessor)</param>
        public static void NotifyState<T>(this T target, object oldValue, [CallerMemberName] string? propertyName = null)
            where T : class, IPropertyChangeNotifier
        {
            if (propertyName is null)
                throw new ArgumentNullException(nameof(propertyName), "The name of the changed property could not be established. Check the " +
                                                                      "context of the call to NotifyChange, and consider providing an explicit " +
                                                                      "'propertyName' parameter.");
            var typeCache = ApplicationServicesAccessor.TypeCache;
            if (!typeCache.GetDeclaredProperties(typeof(T), groupByActualName: true).TryGetValue(propertyName, out DeclaredProperty property))
                throw new ArgumentException($"Could not find a property with actual name '{propertyName}' in type '{typeof(T)}'.");

            property.NotifyChange
            (
                target: target,
                oldValue: oldValue,
                newValue: property.GetValue(target).AsTask().Result
            );
        }
    }
}