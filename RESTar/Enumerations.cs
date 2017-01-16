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

        /// <summary>
        /// Makes GET available on public port and the rest of the
        /// methods available on only the private port.
        /// </summary>
        ReadAndPrivateWrite
    }

    public enum RESTarMethods
    {
        GET,
        POST,
        PATCH,
        PUT,
        DELETE,
        Private_GET,
        Private_POST,
        Private_PATCH,
        Private_PUT,
        Private_DELETE
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
        internal Type Type;

        internal TypeAttribute(Type type)
        {
            Type = type;
        }
    }

    public enum RESTarMetaConditions
    {
        [Type(typeof(decimal))] Limit,
        [Type(typeof(string))] Order_desc,
        [Type(typeof(string))] Order_asc,
        [Type(typeof(bool))] Unsafe,
        [Type(typeof(string))] Select,
        [Type(typeof(string))] Rename,
        [Type(typeof(bool))] Dynamic,
        [Type(typeof(decimal))] Map
    }

    internal enum RESTarMimeType : byte
    {
        Json,
        Excel
    }
}