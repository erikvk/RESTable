using System;
using Starcounter;
using System.Reflection;
using Newtonsoft.Json.Serialization;
using RESTar.Operations;
using static System.Activator;

namespace RESTar.Serialization
{
    /// <inheritdoc />
    /// <summary>
    /// Tries to get the value of a collection, and returns an empty array if the value is null
    /// </summary>
    internal class ArrayNullToEmptyListProvider : IValueProvider
    {
        private readonly PropertyInfo Property;
        public ArrayNullToEmptyListProvider(PropertyInfo property) => Property = property;
        public object GetValue(object target) => Property.GetValue(target) ?? new object[0];
        public void SetValue(object target, object value) => Property.SetValue(target, value);
    }

    /// <inheritdoc />
    /// <summary>
    /// Tries to get the value of a value type, and returns an empty object if the value is null
    /// </summary>
    internal class ClassNullToEmptyObjectProvider : IValueProvider
    {
        private readonly PropertyInfo Property;
        public ClassNullToEmptyObjectProvider(PropertyInfo property) => Property = property;
        public object GetValue(object target) => Property.GetValue(target) ?? new object();
        public void SetValue(object target, object value) => Property.SetValue(target, value);
    }

    /// <summary>
    /// Wraps an object in the RESTar macro for delivery to the view model
    /// </summary>
    internal class RESTarMacroWrapperProvider : IValueProvider
    {
        private readonly PropertyInfo Property;
        public RESTarMacroWrapperProvider(PropertyInfo property) => Property = property;
        public void SetValue(object target, object value) { }

        public object GetValue(object target)
        {
            switch (Property.GetValue(target))
            {
                case DateTime dateTime: return $"@RESTar(\"{dateTime:O}\")";
                case string @string: return $"@RESTar(\"{@string}\")";
                case var other: return $"@RESTar({other})";
            }
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Gets the Starcounter object number for an object
    /// </summary>
    internal class ObjectNoProvider : IValueProvider
    {
        public object GetValue(object target) => Do.Try(target.GetObjectNo, 0UL);
        public void SetValue(object target, object value) { }
    }

    /// <inheritdoc />
    /// <summary>
    /// Gets the Starcounter object ID for an object
    /// </summary>
    internal class ObjectIDProvider : IValueProvider
    {
        public object GetValue(object target) => Do.Try(target.GetObjectID, "");
        public void SetValue(object target, object value) { }
    }

    /// <summary>
    /// Reduces a nullable type to its underlying value type, for use in view models
    /// </summary>
    internal class NullableValueProvider : IValueProvider
    {
        private readonly object Default;
        private readonly IValueProvider Provider;
        public void SetValue(object target, object value) => Provider.SetValue(target, value);
        public object GetValue(object target) => Provider.GetValue(target) ?? Default;
        public NullableValueProvider(MemberInfo m, Type t) => (Provider, Default) = (new DynamicValueProvider(m), CreateInstance(t));
    }

    /// <summary>
    /// Makes the empty string act as the default for strings in view models
    /// </summary>
    internal class NullToEmptyStringValueProvider : IValueProvider
    {
        private readonly PropertyInfo Property;
        public NullToEmptyStringValueProvider(PropertyInfo property) => Property = property;
        public object GetValue(object target) => Property.GetValue(target) ?? "";
        public void SetValue(object target, object value) => Property.SetValue(target, value);
    }
}