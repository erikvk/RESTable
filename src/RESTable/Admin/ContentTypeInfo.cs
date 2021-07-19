using System.Collections.Generic;

namespace RESTable.Admin
{
    /// <summary>
    /// Describes a content type
    /// </summary>
    public class ContentTypeInfo
    {
        /// <summary>
        /// The name of the content type
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The MIME type of this content type
        /// </summary>
        public string MimeType { get; }

        /// <summary>
        /// Can this content type be used to read data?
        /// </summary>
        public bool CanRead { get; }

        /// <summary>
        /// Can this content type be used to write data?
        /// </summary>
        public bool CanWrite { get; }

        /// <summary>
        /// The MIME type string bindings used for the protocol provider
        /// </summary>
        public IEnumerable<string> Bindings { get; }

        public ContentTypeInfo(string name, string mimeType, bool canRead, bool canWrite, IEnumerable<string> bindings)
        {
            Name = name;
            MimeType = mimeType;
            CanRead = canRead;
            CanWrite = canWrite;
            Bindings = bindings;
        }
    }
}