using System;

namespace RESTar
{
    public enum RESTarPresets : byte
    {
        /// <summary>
        /// Makes GET available for this resource
        /// </summary>
        ReadOnly,

        /// <summary>
        /// Makes POST and DELETE available for this resource
        /// </summary>
        WriteOnly,

        /// <summary>
        /// Makes GET and PATCH available for this resource
        /// </summary>
        ReadAndUpdate,

        /// <summary>
        /// Makes all methods available for this resource
        /// </summary>
        ReadAndWrite,
    }

    public enum RESTarMethods
    {
        GET,
        POST,
        PATCH,
        PUT,
        DELETE
    }

    public enum RESTarOperations
    {
        Select,
        Insert,
        Update,
        Delete
    }

    internal class TypeAttribute : Attribute
    {
        internal readonly Type Type;

        internal TypeAttribute(Type type)
        {
            Type = type;
        }
    }

    public enum RESTarMetaConditions
    {
        Limit,
        Order_desc,
        Order_asc,
        Unsafe,
        Select,
        Rename,
        Dynamic,
        Map,
        Imgput,
        Safepost
    }

    internal enum RESTarMimeType : byte
    {
        Json,
        Excel
    }
}