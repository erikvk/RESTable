using System;
using System.IO;

namespace RESTar.Internal
{
    /// <inheritdoc />
    /// <summary>
    /// The main stream class used for output streams from RESTar
    /// </summary>
    internal class RESTarOutputStreamController : Stream
    {
        private const int MaxMemoryContentLength = 1 << 24;
        private bool Swapped;
        internal Stream Stream { get; private set; }

        /// <inheritdoc />
        public RESTarOutputStreamController() => Stream = new MemoryStream(1024);

        private void Swap()
        {
            Position = 0;
            var fileStream = RESTarConfig.MakeTempFile();
            using (var memoryStream = (MemoryStream) Stream)
                memoryStream.WriteTo(fileStream);
            Stream = fileStream;
            Swapped = true;
        }

        /// <inheritdoc />
        public override void Flush() => Stream.Flush();

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin) => Stream.Seek(offset, origin);

        /// <inheritdoc />
        public override void SetLength(long value) => Stream.SetLength(value);

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count) => Stream.Read(buffer, offset, count);

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count)
        {
            var p = Stream.Position;
            if (!Swapped && p + count > MaxMemoryContentLength) Swap();
            Stream.Write(buffer, offset, count);
        }

        /// <inheritdoc />
        public override bool CanRead => Stream.CanRead;

        /// <inheritdoc />
        public override bool CanSeek => Stream.CanSeek;

        /// <inheritdoc />
        public override bool CanWrite => Stream.CanWrite;

        /// <inheritdoc />
        public override long Length => Stream.Length;

        /// <inheritdoc />
        public override long Position
        {
            get => Stream.Position;
            set => Stream.Position = value;
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            Stream.Dispose();
            base.Dispose(disposing);
        }
    }
}