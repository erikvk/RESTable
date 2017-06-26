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
        public bool Dynamic { get; set; }
        public bool AllowDynamicConditions { get; set; }
        public bool Viewable { get; set; }
        public bool Singleton { get; set; }
        public RESTarAttribute(RESTarPresets preset) => AvailableMethods = preset.ToMethods();

        public RESTarAttribute(RESTarPresets preset, params RESTarMethods[] additionalMethods) => AvailableMethods =
            preset.ToMethods().Union(additionalMethods ?? new RESTarMethods[0]).ToArray();

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