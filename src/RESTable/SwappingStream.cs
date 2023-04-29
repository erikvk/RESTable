using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IO;

namespace RESTable;

public class SwappingStream : Stream, IDisposable, IAsyncDisposable
{
    private const int MaxMemoryContentLength = 1 << 24;
    private bool Swapped;

    /// <summary>
    ///     The underlying stream
    /// </summary>
    protected Stream Stream { get; set; }

    public byte[] GetBytes()
    {
        if (!Stream.CanRead) return Array.Empty<byte>();
        try
        {
            return Stream.ToByteArray();
        }
        finally
        {
            Rewind();
        }
    }

    public async Task<byte[]> GetBytesAsync()
    {
        if (!Stream.CanRead) return Array.Empty<byte>();
        try
        {
            return await Stream.ToByteArrayAsync().ConfigureAwait(false);
        }
        finally
        {
            Rewind();
        }
    }

    public SwappingStream Rewind()
    {
        Seek(0, SeekOrigin.Begin);
        return this;
    }

    public SwappingStream()
    {
        Stream = MemoryStreamManager.GetStream();
        Swapped = false;
    }

    public SwappingStream(ReadOnlySpan<byte> bytes)
    {
        Stream = MemoryStreamManager.GetStream(bytes);
        Swapped = false;
    }

    public SwappingStream(Stream? existing)
    {
        Stream = existing ?? MemoryStreamManager.GetStream();
        Swapped = Stream is not MemoryStream;
    }

    private static readonly RecyclableMemoryStreamManager MemoryStreamManager = new();

    private async Task Swap()
    {
        Position = 0;
        var fileStream = MakeTempFile();
        var memoryStream = (MemoryStream) Stream;
        await using (memoryStream.ConfigureAwait(false))
        {
            await memoryStream.CopyToAsync(fileStream).ConfigureAwait(false);
            Stream = fileStream;
            Swapped = true;
        }
    }

    private FileStream MakeTempFile()
    {
        return File.Create
        (
            $"{Path.GetTempPath()}{Guid.NewGuid()}.restable",
            1048576,
            FileOptions.Asynchronous | FileOptions.DeleteOnClose
        );
    }

    public override bool CanRead => Stream.CanRead;
    public override bool CanSeek => Stream.CanSeek;
    public override bool CanWrite => Stream.CanWrite;
    public override long Length => Stream.Length;

    public override long Position
    {
        get => Stream.Position;
        set => Stream.Position = value;
    }

    private bool CheckShouldSwap(int bytesToWrite)
    {
        return !Swapped && Stream is MemoryStream && Stream.Position + bytesToWrite > MaxMemoryContentLength;
    }

    public override void Close()
    {
        Stream.Close();
        base.Close();
    }

    #region Synchronous IO

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

    public override void Write(byte[] buffer, int offset, int count)
    {
        if (CheckShouldSwap(count))
            Swap().Wait();
        Stream.Write(buffer, offset, count);
    }

    #endregion

    #region Asynchronous IO

    public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
    {
        return Stream.CopyToAsync(destination, bufferSize, cancellationToken);
    }

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        return Stream.FlushAsync(cancellationToken);
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return Stream.ReadAsync(buffer, offset, count, cancellationToken);
    }


    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        if (CheckShouldSwap(count))
            await Swap().ConfigureAwait(false);
        await Stream.WriteAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
    }

    #endregion

    public override bool CanTimeout => Stream.CanTimeout;

    public override int ReadTimeout
    {
        get
        {
            try
            {
                return Stream.ReadTimeout;
            }
            catch (NotImplementedException)
            {
                return int.MaxValue;
            }
            catch (InvalidOperationException)
            {
                return int.MaxValue;
            }
        }
        set => Stream.ReadTimeout = value;
    }

    public override void CopyTo(Stream destination, int bufferSize)
    {
        Stream.CopyTo(destination, bufferSize);
    }

    public override int Read(Span<byte> buffer)
    {
        return Stream.Read(buffer);
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        if (CheckShouldSwap(buffer.Length))
            Swap().Wait();
        Stream.Write(buffer);
    }

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = new())
    {
        return Stream.ReadAsync(buffer, cancellationToken);
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = new())
    {
        if (CheckShouldSwap(buffer.Length))
            await Swap().ConfigureAwait(false);
        await Stream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
    }

    public override async ValueTask DisposeAsync()
    {
        await Stream.DisposeAsync().ConfigureAwait(false);
        await base.DisposeAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        Stream.Dispose();
        base.Dispose(disposing);
    }
}
