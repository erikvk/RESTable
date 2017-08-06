using System;
using System.Collections.Generic;
using System.Linq;

namespace RESTar
{
    /// <summary>
    /// Registers a new RESTar resource and provides permissions.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class RESTarAttribute : Attribute
    {
        internal IReadOnlyList<RESTarMethods> AvailableMethods { get; }

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
        public bool Editable { get; set; }

        /// <summary>
        /// Singleton resources get special treatment in the view. They have no list 
        /// view, but only entity view. Good for settings, reports etc.
        /// </summary>
        public bool Singleton { get; set; }

        /// <summary>
        /// Used when creating attributes for dynamic resources
        /// </summary>
        /// <param name="methods"></param>
        internal RESTarAttribute(IReadOnlyList<RESTarMethods> methods) => AvailableMethods = methods;

        /// <summary>
        /// Registers a class as a RESTar resource. If no methods are provided in the 
        /// methods list, all methods will be enabled for this resource.
        /// </summary>
        public RESTarAttribute(params RESTarMethods[] methods)
        {
            if (!methods.Any())
                methods = RESTarConfig.Methods;
            AvailableMethods = methods.OrderBy(i => i, MethodComparer.Instance).ToList();
        }
    }

    /// <summary>
    /// Registers a class as an internal RESTar resource, that can only
    /// be used in internal requests (using the RESTar.Request`1 class).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class RESTarInternalAttribute : RESTarAttribute
    {
        /// <summary>
        /// Used when creating attributes for dynamic resources
        /// </summary>
        /// <param name="methods"></param>
        internal RESTarInternalAttribute(IReadOnlyList<RESTarMethods> methods) : base(methods)
        {
        }

        /// <summary>
        /// Registers a class as a RESTar internal resource. If no methods are provided in the 
        /// methods list, all methods will be enabled for this resource.
        /// </summary>
        public RESTarInternalAttribute(params RESTarMethods[] methods) : base(methods)
        {
        }
    }

    internal class DynamicTableAttribute : Attribute
    {
    }

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
        public Operator[] Operators { get; set; }

        /// <summary>
        /// Creates a new instance ot the AllowedOperators attribute, using the 
        /// provided list of strings to parse allowed operators.
        /// </summary>
        /// <param name="allowedOperators"></param>
        public AllowedConditionOperatorsAttribute(params string[] allowedOperators)
        {
            try
            {
                Operators = allowedOperators.Select(a => (Operator) a).ToArray();
            }
            catch
            {
                throw new Exception("Invalid RESTarMemberAttribute declaration. Invalid operator string in " +
                                    "allowedOperators.");
            }
        }
    }

    /// <summary>
    /// An attribute that can be used to decorate field and property declarations, and tell
    /// the serializer to flatten them using the ToString() method when writing to excel.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ExcelFlattenToStringAttribute : Attribute
    {
    }

    /// <summary>
    /// Resources decorated with the OpenResourceAttribute will be available to all users,
    /// unless the user's API key explicitly denies access to them. By assigning methods 
    /// or presets in the constructor, access can be restricted. Open resources are useful 
    /// when providing basic functionality that should be consumed by all users.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class OpenResourceAttribute : Attribute
    {
        internal IReadOnlyList<RESTarMethods> AvailableMethods { get; }

        /// <summary>
        /// If no methods are provided in the methods list, all methods enabled for the resource 
        /// will be enabled
        /// </summary>
        public OpenResourceAttribute(params RESTarMethods[] methods)
        {
            AvailableMethods = methods.Any() ? methods.OrderBy(i => i, MethodComparer.Instance).ToList() : null;
        }
    }
}