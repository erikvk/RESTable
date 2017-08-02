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
        /// </summary>
        /// <param name="preset">A preset used for setting up available methods for the resource</param>
        public RESTarAttribute(RESTarPresets preset) => AvailableMethods = preset.ToMethods();

        /// <summary>
        /// Used when creating attributes for dynamic resources
        /// </summary>
        /// <param name="methods"></param>
        internal RESTarAttribute(IReadOnlyList<RESTarMethods> methods) => AvailableMethods = methods;

        /// <summary>
        /// </summary>
        /// <param name="preset">A preset used for setting up available methods for the resource</param>
        /// <param name="additionalMethods">Additional methods for this resource, apart from the one defined by
        /// the preset</param>
        public RESTarAttribute(RESTarPresets preset, params RESTarMethods[] additionalMethods)
        {
            var methods = preset.ToMethods().Union(additionalMethods ?? new RESTarMethods[0]).ToList();
            methods.Sort(MethodComparer.Instance);
            AvailableMethods = methods;
        }

        /// <summary>
        /// </summary>
        /// <param name="method">A method to make available for the resource</param>
        /// <param name="addMethods">Additional methods to make available for the resource</param>
        public RESTarAttribute(RESTarMethods method, params RESTarMethods[] addMethods)
        {
            var methods = new[] {method}.Union(addMethods ?? new RESTarMethods[0]).ToList();
            methods.Sort(MethodComparer.Instance);
            AvailableMethods = methods;
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
        /// </summary>
        /// <param name="preset">A preset used for setting up available methods for the resource</param>
        public RESTarInternalAttribute(RESTarPresets preset) : base(preset)
        {
        }

        /// <summary>
        /// Used when creating attributes for dynamic resources
        /// </summary>
        /// <param name="methods"></param>
        internal RESTarInternalAttribute(IReadOnlyList<RESTarMethods> methods) : base(methods)
        {
        }

        /// <summary>
        /// </summary>
        /// <param name="preset">A preset used for setting up available methods for the resource</param>
        /// <param name="additionalMethods">Additional methods for this resource, apart from the one defined by
        /// the preset</param>
        public RESTarInternalAttribute(RESTarPresets preset, params RESTarMethods[] additionalMethods) : base(preset,
            additionalMethods)
        {
        }

        /// <summary>
        /// </summary>
        /// <param name="method">A method to make available for the resource</param>
        /// <param name="addMethods">Additional methods to make available for the resource</param>
        public RESTarInternalAttribute(RESTarMethods method, params RESTarMethods[] addMethods) : base(method,
            addMethods)
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
}