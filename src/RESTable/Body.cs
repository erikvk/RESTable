using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RESTable.ContentTypeProviders;
using RESTable.Results;

namespace RESTable;

/// <inheritdoc cref="IDisposable" />
/// <summary>
///     Encodes a request body
/// </summary>
public class Body : Stream, IDisposable, IAsyncDisposable
{
    private SwappingStream Stream { get; set; }

    private IProtocolHolder ProtocolHolder { get; }

    private bool IsIngoing { get; }

    private IAsyncEnumerable<object>? UninitializedAsyncBodyObject { get; set; }

    public ContentType ContentType => IsIngoing
        ? ProtocolHolder.InputContentTypeProvider.ContentType
        : ProtocolHolder.OutputContentTypeProvider.ContentType;

    private IContentTypeProvider ContentTypeProvider => IsIngoing
        ? ProtocolHolder.InputContentTypeProvider
        : ProtocolHolder.OutputContentTypeProvider;

    /// <summary>
    ///     Serializes the given result object to this body, using the appropriate content type
    ///     provider assigned to the request or response that this body belongs to.
    /// </summary>
    /// <param name="result"></param>
    public async Task Serialize(ISerializedResult result, CancellationToken cancellationToken)
    {
        await ProtocolHolder.CachedProtocolProvider.ProtocolProvider.SerializeResult(result, ContentTypeProvider, cancellationToken).ConfigureAwait(false);
        TryRewind();
    }

    /// <summary>
    ///     Deserializes the body to an IAsyncEnumerable of entities of the given type
    /// </summary>
    public IAsyncEnumerable<T> DeserializeAsyncEnumerable<T>(CancellationToken cancellationToken = new())
    {
        if (IsClosed) throw new ObjectDisposedException(nameof(Stream));
        try
        {
            return ContentTypeProvider.DeserializeAsyncEnumerable<T>(Stream, cancellationToken);
        }
        finally
        {
            if (CanSeek) Stream.Rewind();
        }
    }


    /// <summary>
    ///     Deserializes the body to an IAsyncEnumerable of entities of the given type
    /// </summary>
    public ValueTask<T?> DeserializeAsync<T>(CancellationToken cancellationToken = new())
    {
        if (IsClosed) throw new ObjectDisposedException(nameof(Stream));
        try
        {
            return ContentTypeProvider.DeserializeAsync<T>(Stream, cancellationToken);
        }
        finally
        {
            if (CanSeek) Stream.Rewind();
        }
    }

    /// <summary>
    ///     Populates the body onto each entity in a source collection. If the body is empty,
    ///     returns null.
    /// </summary>
    public IAsyncEnumerable<T> PopulateTo<T>(IAsyncEnumerable<T> source, CancellationToken cancellationToken) where T : notnull
    {
        if (IsClosed)
            throw new ObjectDisposedException(nameof(Stream));
        try
        {
            return ContentTypeProvider.Populate(source, Stream, cancellationToken);
        }
        finally
        {
            if (CanSeek)
                Stream.Rewind();
        }
    }

    /// <summary>
    ///     Has this body been closed due to a disposal of request or result?
    /// </summary>
    public bool IsClosed => !Stream.CanRead;

    /// <summary>
    ///     The length of the body content in bytes
    ///     ///
    /// </summary>
    public long ContentLength => Stream.Length;

    /// <summary>
    ///     Creates a body object to be used in a RESTable request
    /// </summary>
    /// <param name="protocolHolder">The protocol holder, for example a request</param>
    /// <param name="bodyObject">The object to initialize this body from</param>
    public Body(IProtocolHolder protocolHolder, object? bodyObject = null)
    {
        ProtocolHolder = protocolHolder;
        IsIngoing = true;
        Stream = ResolveStream(bodyObject);
    }

    /// <summary>
    ///     Only for outgoing streams
    /// </summary>
    private Body(IProtocolHolder protocolHolder, Stream? customOutputStream)
    {
        ProtocolHolder = protocolHolder;
        IsIngoing = false;
        Stream = new SwappingStream(customOutputStream);
    }

    private SwappingStream ResolveStream(object? bodyObject)
    {
        var jsonProvider = ProtocolHolder.Context.GetRequiredService<IJsonProvider>();

        switch (bodyObject)
        {
            case null: return new SwappingStream();
            case string str: return new SwappingStream(str.ToBytes());
            case Memory<byte> bytes: return new SwappingStream(bytes.Span);
            case ReadOnlyMemory<byte> bytes: return new SwappingStream(bytes.ToArray());
            case ArraySegment<byte> bytes: return new SwappingStream(bytes);
            case ReadOnlySequence<byte> bytes: return new SwappingStream(bytes.ToArray());
            case byte[] bytes: return new SwappingStream(bytes);

            case Body body:
            {
                ProtocolHolder.Headers.ContentType = body.ContentType;
                body.TryRewind();
                return body.Stream;
            }
            case SwappingStream swappingStream: return swappingStream.Rewind();
            case Stream otherStream: return new SwappingStream(otherStream);

            // We don't write this to a stream in a synchronous context. Instead we wait until Initialize() is called
            case IAsyncEnumerable<object> asyncEnumerable:
            {
                UninitializedAsyncBodyObject = asyncEnumerable;
                return new SwappingStream();
            }
            case { } other:
            {
                var json = jsonProvider.SerializeToUtf8Bytes(other, other.GetType(), false);
                return new SwappingStream(json);
            }
        }
    }

    public async Task Initialize(CancellationToken cancellationToken)
    {
        if (UninitializedAsyncBodyObject is null)
            return;
        var jsonProvider = ProtocolHolder.Context.GetRequiredService<IJsonProvider>();
        await jsonProvider.SerializeAsyncEnumerable(Stream, UninitializedAsyncBodyObject, cancellationToken).ConfigureAwait(false);
        Stream.Rewind();
        UninitializedAsyncBodyObject = null;
    }

    internal static Body CreateOutputBody(IProtocolHolder protocolHolder, Stream? customOutputStream)
    {
        return new Body(protocolHolder, customOutputStream);
    }

    private const int MaxStringLength = 10_000;

    public string GetLengthLogString()
    {
        return IsClosed && !TryRewind() ? "" : $" ({ContentLength} bytes)";
    }

    /// <summary>
    ///     Writes the body to a string
    /// </summary>
    /// <returns></returns>
    public async Task<string> ToStringAsync()
    {
        if (IsClosed)
            return "";
        TryRewind();
        try
        {
            using var reader = new StreamReader(Stream, Encoding.Default, false, 1024, true);
            var stringBuilder = new StringBuilder();
            var buffer = new char[1024];
            var charsLeft = MaxStringLength;
            while (charsLeft > 0)
            {
                var readChars = await reader.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                if (readChars == 0) break;
                charsLeft -= readChars;
                stringBuilder.Append(buffer, 0, readChars);
            }
            return stringBuilder.ToString();
        }
        finally
        {
            TryRewind();
        }
    }

    public override string ToString()
    {
        return ToStringAsync().Result;
    }

    public async Task<Body> GetCopy()
    {
        if (IsClosed)
            throw new ObjectDisposedException(nameof(Stream), "The body has been disposed. Likely, the request or the result was disposed before serialization began");
        var copy = new Body(ProtocolHolder);
        await Stream.CopyToAsync(copy.Stream).ConfigureAwait(false);
        copy.Stream.Rewind();
        TryRewind();
        return copy;
    }

    internal bool TryRewind()
    {
        if (!Stream.CanSeek)
            return false;
        Stream.Rewind();
        return true;
    }

    protected override void Dispose(bool disposing)
    {
        Stream.Dispose();
        base.Dispose(disposing);
    }

#if NETSTANDARD2_0
    public async ValueTask DisposeAsync()
    {
        base.Dispose(false);
        await Stream.DisposeAsync().ConfigureAwait(false);
    }
#else
    public override async ValueTask DisposeAsync()
    {
        base.Dispose(false);
        await Stream.DisposeAsync().ConfigureAwait(false);
    }
#endif

    internal byte[] GetBytes()
    {
        return Stream.GetBytes();
    }

    internal Task<byte[]> GetBytesAsync()
    {
        return Stream.GetBytesAsync();
    }

    #region Stream

    public override int ReadTimeout
    {
        get => Stream.ReadTimeout;
        set => Stream.ReadTimeout = value;
    }

    public override int WriteTimeout
    {
        get => Stream.WriteTimeout;
        set => Stream.WriteTimeout = value;
    }

    public override long Position
    {
        get => Stream.Position;
        set => Stream.Position = value;
    }

    public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
    {
        return Stream.CopyToAsync(destination, bufferSize, cancellationToken);
    }

    public override int EndRead(IAsyncResult asyncResult)
    {
        return Stream.EndRead(asyncResult);
    }

    public override void EndWrite(IAsyncResult asyncResult)
    {
        Stream.EndWrite(asyncResult);
    }

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        return Stream.FlushAsync(cancellationToken);
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return Stream.ReadAsync(buffer, offset, count, cancellationToken);
    }

    public override int ReadByte()
    {
        return Stream.ReadByte();
    }

    public override void WriteByte(byte value)
    {
        Stream.WriteByte(value);
    }

    public override bool CanTimeout => Stream.CanTimeout;

    public override void Flush()
    {
        Stream.Flush();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        return Stream.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
        Stream.SetLength(value);
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return Stream.Read(buffer, offset, count);
    }

    public override bool CanRead => Stream.CanRead;
    public override bool CanSeek => Stream.CanSeek;
    public override bool CanWrite => Stream.CanWrite;
    public override long Length => Stream.Length;

    public override void Close()
    {
        Stream.Close();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        Stream.Write(buffer, offset, count);
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return Stream.WriteAsync(buffer, offset, count, cancellationToken);
    }

#if !NETSTANDARD2_0
    public override void CopyTo(Stream destination, int bufferSize)
    {
        Stream.CopyTo(destination, bufferSize);
    }

    public override int Read(Span<byte> buffer)
    {
        return Stream.Read(buffer);
    }

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = new())
    {
        return Stream.ReadAsync(buffer, cancellationToken);
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        Stream.Write(buffer);
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = new())
    {
        return Stream.WriteAsync(buffer, cancellationToken);
    }
#endif

    #endregion
}