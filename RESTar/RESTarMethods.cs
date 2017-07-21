using System;
using System.Collections.Generic;

namespace RESTar
{
    /// <summary>
    /// The REST methods available in RESTar
    /// </summary>
    public enum RESTarMethods
    {
        /// <summary>
        /// GET, returns entities from a resource
        /// </summary>
        GET,

        /// <summary>
        /// POST, inserts entities into a resource
        /// </summary>
        POST,

        /// <summary>
        /// PATCH, updates existing entities in a resource
        /// </summary>
        PATCH,

        /// <summary>
        /// PUT, tries to locate a resource entity. If no one was found, 
        /// inserts a new entity. If one was found, updates that entity.
        /// If more than one was found, returns an error.
        /// </summary>
        PUT,

        /// <summary>
        /// DELETE, deletes one or more entities from a resource
        /// </summary>
        DELETE
    }

    internal class MethodComparer : Comparer<RESTarMethods>
    {
        internal static MethodComparer Instance = new MethodComparer();

        public override int Compare(RESTarMethods a, RESTarMethods b)
        {
            var indexA = Array.IndexOf(RESTarConfig.Methods, a);
            var indexB = Array.IndexOf(RESTarConfig.Methods, b);
            return indexA < indexB ? -1 : (indexB < indexA ? 1 : 0);
        }
    }
}