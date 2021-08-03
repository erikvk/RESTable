using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IO;

namespace RESTable
{
    public class SwappingStream : Stream, IDisposable, IAsyncDisposable
    {
        private const int MaxMemoryContentLength = 1 << 24;
        private bool Swapped;

        /// <summary>
        /// The underlying stream
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

        public SwappingStream(byte[] bytes)
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
#if NETSTANDARD2_0
            using (memoryStream)
#else
            await using (memoryStream.ConfigureAwait(false))
#endif
            {
                await memoryStream.CopyToAsync(fileStream).ConfigureAwait(false);
                Stream = fileStream;
                Swapped = true;
            }
        }

        private FileStream MakeTempFile() => File.Create
        (
            path: $"{Path.GetTempPath()}{Guid.NewGuid()}.restable",
            bufferSize: 1048576,
            options: FileOptions.Asynchronous | FileOptions.DeleteOnClose
        );

        public override bool CanRead => Stream.CanRead;
        public override bool CanSeek => Stream.CanSeek;
        public override bool CanWrite => Stream.CanWrite;
        public override long Length => Stream.Length;
        public override bool CanTimeout => Stream.CanTimeout;

        public override long Position
        {
            get => Stream.Position;
            set => Stream.Position = value;
        }

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

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) =>
            Stream.BeginRead(buffer, offset, count, callback, state);

        public override int EndRead(IAsyncResult asyncResult) => Stream.EndRead(asyncResult);
        public override void EndWrite(IAsyncResult asyncResult) => Stream.EndWrite(asyncResult);
        public override int ReadByte() => Stream.ReadByte();
        public override void Flush() => Stream.Flush();
        public override long Seek(long offset, SeekOrigin origin) => Stream.Seek(offset, origin);
        public override void SetLength(long value) => Stream.SetLength(value);
        public override int Read(byte[] buffer, int offset, int count) => Stream.Read(buffer, offset, count);

        public override void WriteByte(byte value)
        {
            if (CheckShouldSwap(1))
                Swap().Wait();
            Stream.WriteByte(value);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        {
            if (CheckShouldSwap(count))
                Swap().Wait();
            return Stream.BeginWrite(buffer, offset, count, callback, state);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (CheckShouldSwap(count))
                Swap().Wait();
            Stream.Write(buffer, offset, count);
        }

        #endregion

        #region Asynchronous IO

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) => Stream.CopyToAsync(destination, bufferSize, cancellationToken);
        public override Task FlushAsync(CancellationToken cancellationToken) => Stream.FlushAsync(cancellationToken);

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            Stream.ReadAsync(buffer, offset, count, cancellationToken);


        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (CheckShouldSwap(count))
                await Swap().ConfigureAwait(false);
            await Stream.WriteAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
        }

        #endregion

#if NETSTANDARD2_0
        public ValueTask DisposeAsync()
        {
            Stream.Dispose();
            base.Dispose();
            return default;
        }
#else
        public override void CopyTo(Stream destination, int bufferSize) => Stream.CopyTo(destination, bufferSize);
        public override int Read(Span<byte> buffer) => Stream.Read(buffer);

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            if (CheckShouldSwap(buffer.Length))
                Swap().Wait();
            Stream.Write(buffer);
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = new()) => Stream.ReadAsync(buffer, cancellationToken);

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
#endif

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            Stream.Dispose();
            base.Dispose(disposing);
        }
    }
}