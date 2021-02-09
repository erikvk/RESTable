using System;
using System.Diagnostics.CodeAnalysis;
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
        [NotNull]
        protected Stream Stream { get; set; }

        internal byte[] GetBytes()
        {
            if (!Stream.CanRead) return new byte[0];
            try
            {
                return Stream.ToByteArray();
            }
            finally
            {
                this.Rewind();
            }
        }

        internal async Task<byte[]> GetBytesAsync()
        {
            if (!Stream.CanRead) return new byte[0];
            this.Rewind();
            try
            {
                return await Stream.ToByteArrayAsync();
            }
            finally
            {
                this.Rewind();
            }
        }

        internal SwappingStream Rewind()
        {
            Seek(0, SeekOrigin.Begin);
            return this;
        }

        internal async Task MakeSeekable()
        {
            if (Stream.CanSeek) return;
            if (Swapped)
                throw new InvalidOperationException("Could not make stream seekable");
            await Swap();
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

        public SwappingStream(Stream existing)
        {
            Stream = existing ?? MemoryStreamManager.GetStream();
            Swapped = Stream is not MemoryStream;
        }

        private static readonly RecyclableMemoryStreamManager MemoryStreamManager = new();

        private async Task Swap()
        {
            Position = 0;
            var fileStream = RESTableConfig.MakeTempFile();
            await using var memoryStream = (MemoryStream) Stream;
            memoryStream.WriteTo(fileStream);
            Stream = fileStream;
            Swapped = true;
        }

        internal bool CanClose { get; set; }

        #region Synchronous IO

        public override void Flush() => Stream.Flush();
        public override long Seek(long offset, SeekOrigin origin) => Stream.Seek(offset, origin);
        public override void SetLength(long value) => Stream.SetLength(value);
        public override int Read(byte[] buffer, int offset, int count) => Stream.Read(buffer, offset, count);
        public override bool CanRead => Stream.CanRead;
        public override bool CanSeek => Stream.CanSeek;
        public override bool CanWrite => Stream.CanWrite;
        public override long Length => Stream.CanSeek ? Stream.Length : -1;

        public override void Close()
        {
            if (!CanClose) return;
            Stream.Close();
            base.Close();
        }

        private bool CheckShouldSwap(int bytesToWrite)
        {
            return !Swapped && Stream is MemoryStream && Stream.Position + bytesToWrite > MaxMemoryContentLength;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (CheckShouldSwap(count))
                Swap().Wait();
            Stream.Write(buffer, offset, count);
        }

        public override long Position
        {
            get => Stream.Position;
            set => Stream.Position = value;
        }

        #endregion

        #region async IO

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (CheckShouldSwap(count))
                await Swap();
            await Stream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = new())
        {
            if (CheckShouldSwap(buffer.Length))
                await Swap();
            await Stream.WriteAsync(buffer, cancellationToken);
        }

        #endregion

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (!CanClose) return;
            Stream.Dispose();
            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            if (!CanClose) return;
            await Stream.DisposeAsync();
            await base.DisposeAsync();
        }
    }
}