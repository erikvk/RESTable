using System.Collections.Generic;

namespace RESTable
{
    /// <inheritdoc />
    /// <summary>
    /// A collection of ContentType instances
    /// </summary>
    public class ContentTypes : List<ContentType>
    {
        /// <inheritdoc />
        public override string ToString() => string.Join(",", this);

        /// <inheritdoc />
        public ContentTypes() { }

        /// <inheritdoc />
        public ContentTypes(IEnumerable<ContentType> collection) : base(collection) { }

        /// <summary>
        /// Parses a header value, possibly containing multiple content types, an returnes a 
        /// ContentTypes collection describing them.
        /// </summary>
        public static ContentTypes Parse(string headerValue) => ContentType.ParseMany(headerValue);

        /// <summary>
        /// Creates a ContentTypes from a single ContentType instance
        /// </summary>
        /// <param name="contentType"></param>
        public static implicit operator ContentTypes(ContentType contentType) => new() {contentType};

        /// <summary>
        /// Creates a ContentTypes from an array of ContentType instances
        /// </summary>
        /// <param name="contentTypes"></param>
        public static implicit operator ContentTypes(ContentType[] contentTypes) => new(contentTypes);

        /// <summary>
        /// Converts a header value string to a ContenType
        /// </summary>
        public static implicit operator ContentTypes(string headerValue) => ContentType.ParseMany(headerValue);
    }
}