using System;
using System.Collections.Generic;
using System.Linq;

namespace RESTar
{
    /// <inheritdoc />
    /// <summary>
    /// Registers a new RESTar resource and provides permissions.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class RESTarAttribute : Attribute
    {
        internal IReadOnlyList<Methods> AvailableMethods { get; }

        /// <summary>
        /// If true, unknown conditions encountered when handling incoming requests
        /// will be passed through as dynamic. This allows for a dynamic handling of
        /// members, both for condition matching and for entities returned from the 
        /// resource selector.
        /// </summary>
        public bool AllowDynamicConditions { get; set; }

        /// <summary>
        /// Should this resource be editable after registration?
        /// </summary>
        internal bool Editable { get; set; }

        /// <summary>
        /// Singleton resources get special treatment in the view. They have no list 
        /// view, but only entity view. Good for settings, reports etc.
        /// </summary>
        public bool Singleton { get; set; }

        /// <summary>
        /// Resource descriptions are visible in the AvailableMethods resource
        /// </summary>
        public string Description { get; set; }

        /// <inheritdoc />
        internal RESTarAttribute(IReadOnlyList<Methods> methods) => AvailableMethods = methods;

        /// <inheritdoc />
        public RESTarAttribute(params Methods[] methodRestrictions)
        {
            if (!methodRestrictions.Any())
                methodRestrictions = RESTarConfig.Methods;
            AvailableMethods = methodRestrictions.OrderBy(i => i, MethodComparer.Instance).ToList();
        }
    }

    internal static class RESTarAttribute<T> where T : class
    {
        internal static RESTarAttribute Get => typeof(T).GetAttribute<RESTarAttribute>();
    }

    /// <inheritdoc />
    /// <summary>
    /// Registers a class as an internal RESTar resource, that can only
    /// be used in internal requests (using the RESTar.Request`1 class).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class RESTarInternalAttribute : RESTarAttribute
    {
        /// <inheritdoc />
        internal RESTarInternalAttribute(IReadOnlyList<Methods> methods) : base(methods)
        {
        }

        /// <inheritdoc />
        public RESTarInternalAttribute(params Methods[] methodRestrictions) : base(methodRestrictions)
        {
        }
    }

    internal class DynamicTableAttribute : Attribute
    {
    }

    /// <summary>
    /// Makes a resource property with a public setter read only over the REST API
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public abstract class ReadOnlyAttribute : Attribute
    {
    }

    /// <inheritdoc />
    /// <summary>
    /// An attribute that can be used to decorate field and property declarations, and assign
    /// allowed operators for use in conditions that reference them.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class AllowedConditionOperatorsAttribute : Attribute
    {
        /// <summary>
        /// Only these operators will be allowed in conditions targeting this property
        /// </summary>
        public Operator[] Operators { get; }

        /// <inheritdoc />
        public AllowedConditionOperatorsAttribute(params Operator[] allowedOperators)
        {
            Operators = allowedOperators;
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// An attribute that can be used to decorate field and property declarations, and tell
    /// the serializer to flatten them using the ToString() method when writing to excel.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ExcelFlattenToStringAttribute : Attribute
    {
    }
}