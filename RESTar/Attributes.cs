﻿using System;
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
                    AvailableMethods = new[]
                    {
                        RESTarMethods.GET
                    };
                    break;
                case RESTarPresets.WriteOnly:
                    AvailableMethods = new[]
                    {
                        RESTarMethods.POST,
                        RESTarMethods.DELETE
                    };
                    break;
                case RESTarPresets.ReadAndUpdate:
                    AvailableMethods = new[]
                    {
                        RESTarMethods.GET,
                        RESTarMethods.PATCH
                    };
                    break;
                case RESTarPresets.ReadAndWrite:
                    AvailableMethods = RESTarConfig.Methods;
                    break;
                case RESTarPresets.ReadAndPrivateWrite:
                    AvailableMethods = new[]
                    {
                        RESTarMethods.GET,
                        RESTarMethods.Private_POST,
                        RESTarMethods.Private_PUT,
                        RESTarMethods.Private_PATCH,
                        RESTarMethods.Private_DELETE
                    };
                    break;
            }
        }

        public RESTarAttribute(RESTarMethods method, params RESTarMethods[] addMethods)
        {
            AvailableMethods = new[] {method}.Union(addMethods.Distinct()).ToArray();
        }
    }

    public class ObjectRefAttribute : Attribute
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
}