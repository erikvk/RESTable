using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RESTable.Resources;

public class BinaryResult
{
    public Func<Stream, CancellationToken, Task> WriteToStream { get; }
    public ContentType ContentType { get; }
    public long? ContentLength { get; }
    public string? ContentDisposition { get; }
    public string? Etag { get; }

    public BinaryResult
    (
        string utf8Data,
        string contentType = "text/plain",
        string? contentDisposition = null,
        string? etag = null
    ) : this
    (
        data: Encoding.UTF8.GetBytes(utf8Data),
        contentType: contentType,
        contentDisposition: contentDisposition,
        etag: etag
    ) { }

    public BinaryResult
    (
        ReadOnlyMemory<byte> data,
        ContentType contentType,
        string? contentDisposition = null,
        string? etag = null
    ) : this
    (
        writeToStream: async (stream, ct) => await stream.WriteAsync(data, ct).ConfigureAwait(false),
        contentType: contentType,
        contentLength: data.Length,
        contentDisposition: contentDisposition,
        etag: etag
    ) { }

    public BinaryResult
    (
        Func<Stream, CancellationToken, Task> writeToStream,
        ContentType contentType,
        long? contentLength = null,
        string? contentDisposition = null,
        string? etag = null
    )
    {
        WriteToStream = writeToStream;
        ContentType = contentType;
        ContentLength = contentLength;
        ContentDisposition = contentDisposition;
        Etag = etag;
    }
}
