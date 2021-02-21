using System;
using System.IO;
using System.Threading.Tasks;

namespace RESTable.Resources
{
    public class BinaryResult
    {
        public Func<Stream, Task> WriteToStream { get; }
        public ContentType ContentType { get; }

        public BinaryResult(Func<Stream, Task> writeToStream, ContentType contentType)
        {
            WriteToStream = writeToStream;
            ContentType = contentType;
        }
    }
}