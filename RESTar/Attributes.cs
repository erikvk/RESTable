using System;
using System.Linq;

namespace RESTar
{
    /// <summary>
    /// Registers a new RESTar resource and provides permissions.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class RESTarAttribute : Attribute
    {
        internal RESTarMethods[] AvailableMethods { get; private set; }

        /// <summary>
        /// If true, unknown conditions encountered when handling incoming requests
        /// will be passed through as dynamic. This allows for a dynamic handling of
        /// members, both for condition matching and for entities returned from the 
        /// resource selector.
        /// </summary>
        public bool AllowDynamicConditions { get; set; }

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
        /// </summary>
        /// <param name="preset">A preset used for setting up available methods for the resource</param>
        /// <param name="additionalMethods">Additional methods for this resource, apart from the one defined by
        /// the preset</param>
        public RESTarAttribute(RESTarPresets preset, params RESTarMethods[] additionalMethods) => AvailableMethods =
            preset.ToMethods().Union(additionalMethods ?? new RESTarMethods[0]).ToArray();

        /// <summary>
        /// </summary>
        /// <param name="method">A method to make available for the resource</param>
        /// <param name="addMethods">Additional methods to make available for the resource</param>
        public RESTarAttribute(RESTarMethods method, params RESTarMethods[] addMethods) => AvailableMethods =
            new[] {method}.Union(addMethods ?? new RESTarMethods[0]).ToArray();
    }

    /// <summary>
    /// An attribute that, when used on a property, flattens that 
    /// property using its ToString() method when writing to Excel.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ExcelFlattenToString : Attribute
    {
    }

    internal class DynamicTableAttribute : Attribute
    {
        public int Nr;
        public DynamicTableAttribute(int nr) => Nr = nr;
    }
}