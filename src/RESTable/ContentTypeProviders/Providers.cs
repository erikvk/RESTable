﻿namespace RESTable.ContentTypeProviders
{
    /// <summary>
    /// A static class that provides access to a static JsonProvider
    /// </summary>
    public static class Providers
    {
        /// <summary>
        /// A statically accessable JsonContentProvider
        /// </summary>
        public static NewtonsoftJsonProvider Json { get; }

        static Providers()
        {
            Json = new NewtonsoftJsonProvider();
        }
    }
}