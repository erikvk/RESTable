using System;
using System.Collections.Generic;
using System.Linq;
using Jil;
using RESTar;

namespace RESTar
{
    /// <summary>
    /// Registers a new RESTar resource and provides permissions.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class RESTarAttribute : Attribute
    {
        public RESTarMethods[] AvailableMethods { get; private set; }

        public RESTarAttribute(RESTarPresets preset)
        {
            SetAvailableMethodsFromPreset(preset);
        }

        public RESTarAttribute(RESTarPresets preset, params RESTarMethods[] additionalMethods)
        {
            SetAvailableMethodsFromPreset(preset);
            AvailableMethods = AvailableMethods.Union(additionalMethods).ToArray();
        }

        public void SetAvailableMethodsFromPreset(RESTarPresets preset)
        {
            switch (preset)
            {
                case RESTarPresets.ReadOnly:
                    AvailableMethods = new[] {RESTarMethods.GET};
                    break;
                case RESTarPresets.WriteOnly:
                    AvailableMethods = new[] {RESTarMethods.POST, RESTarMethods.DELETE};
                    break;
                case RESTarPresets.ReadAndUpdate:
                    AvailableMethods = new[] {RESTarMethods.GET, RESTarMethods.PATCH};
                    break;
                case RESTarPresets.ReadAndWrite:
                    AvailableMethods = Config.Methods;
                    break;
            }
        }

        public RESTarAttribute(params RESTarMethods[] customMethodSet)
        {
            AvailableMethods = customMethodSet.Distinct().ToArray();
        }
    }

    /// <summary>
    /// A member decorated with this attribute will be ignored during Jil serialization,
    /// and thus left out while serializing the object into a JSON string.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Field)]
    public class RESTarIgnoreAttribute : JilDirectiveAttribute
    {
        public RESTarIgnoreAttribute()
        {
            Ignore = true;
        }
    }

    /// <summary>
    /// A member decorated with the rename attribute will be serialized to (and deserialized
    /// from) a member with the given name in the JSON tree during Jil serialization. This is 
    /// useful for serializing and deserializing JSON trees containing reserved keywords or 
    /// otherwise unsuitable strings names as member names (keys).
    /// </summary>
    public class rename : JilDirectiveAttribute
    {
        public rename(string name)
        {
            Name = name;
        }
    }

    /// <summary>
    /// Members of enum types can be decorated with the cast attribute to indicate how the 
    /// serializer should treat them during serialization. By default Jil will serialize these 
    /// members to string, this can be overriden to any number type using this attribute.
    /// </summary>
    public class cast : JilDirectiveAttribute
    {
        public cast(Type type)
        {
            TreatEnumerationAs = type;
        }
    }
}