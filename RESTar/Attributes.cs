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

        public RESTarAttribute(RESTarPresets preset)
        {
            AvailableMethods = preset.ToMethods();
        }

        public RESTarAttribute(RESTarPresets preset, params RESTarMethods[] additionalMethods)
        {
            AvailableMethods = preset.ToMethods().Union(additionalMethods).ToArray();
        }

        public RESTarAttribute(RESTarMethods method, params RESTarMethods[] addMethods)
        {
            AvailableMethods = new[] {method}.Union(addMethods).ToArray();
        }
    }

    public class ObjectRefAttribute : Attribute
    {
    }

    public class ExcelFlattenToString : Attribute
    {
    }

    public class DynamicTableAttribute : Attribute
    {
        public int Nr;

        public DynamicTableAttribute(int nr)
        {
            Nr = nr;
        }
    }

    public class OverrideAttribute : Attribute
    {
    }
}