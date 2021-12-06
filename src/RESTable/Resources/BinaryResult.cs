using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace RESTable.Resources;

public class BinaryResult
{
    public BinaryResult(Func<Stream, CancellationToken, Task> writeToStream, ContentType contentType, long? contentLength = null, string? contentDisposition = null)
    {
        WriteToStream = writeToStream;
        ContentType = contentType;
        ContentLength = contentLength;
        ContentDisposition = contentDisposition;
    }

    public Func<Stream, CancellationToken, Task> WriteToStream { get; }
    public ContentType ContentType { get; }
    public long? ContentLength { get; }
    public string? ContentDisposition { get; }
}