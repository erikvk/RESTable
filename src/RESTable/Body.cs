using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RESTable.ContentTypeProviders;
using RESTable.Internal;

namespace RESTable
{
    /// <inheritdoc cref="IDisposable" />
    /// <summary>
    /// Encodes a request body
    /// </summary>
    public class Body : Stream, IAsyncDisposable
    {
        [NotNull] private SwappingStream Stream { get; }

        private IProtocolHolder ProtocolHolder { get; }

        private bool IsIngoing { get; }

        public ContentType ContentType => IsIngoing
            ? ProtocolHolder.GetInputContentTypeProvider().ContentType
            : ProtocolHolder.GetOutputContentTypeProvider().ContentType;

        private IContentTypeProvider ContentTypeProvider => IsIngoing
            ? ProtocolHolder.GetInputContentTypeProvider()
            : ProtocolHolder.GetOutputContentTypeProvider();

        /// <summary>
        /// Deserializes the body to an IEnumerable of entities of the given type
        /// </summary>
        public IEnumerable<T> Deserialize<T>()
        {
            if (!HasContent) return null;
            try
            {
                return ContentTypeProvider.DeserializeCollection<T>(Stream.Rewind());
            }
            finally
            {
                Stream.Rewind();
            }
        }

        /// <summary>   
        /// Populates the body onto each entity in a source collection. If the body is empty,
        /// returns null.
        /// </summary>
        public IEnumerable<T> PopulateTo<T>(IEnumerable<T> source) where T : class
        {
            if (source == null || !HasContent) return null;
            try
            {
                return ContentTypeProvider.Populate(source, Stream.GetBytes());
            }
            finally
            {
                Stream.Rewind();
            }
        }

        /// <summary>
        /// Does this Body have content?
        /// </summary>
        public bool HasContent => Stream switch
        {
            null => false,
            Stream {CanRead: false} => false,
            Stream {CanSeek: true} seekable => seekable.Length > 0,
            _ => true
        };

        /// <summary>
        /// The length of the body content in bytes
        /// /// </summary>
        public long? ContentLength => Stream?.Length;

        public Task MakeSeekable() => Stream.MakeSeekable();

        public Body(IProtocolHolder protocolHolder, object bodyObject = null)
        {
            ProtocolHolder = protocolHolder;
            IsIngoing = true;
            Stream = ResolveStream(bodyObject);
        }

        private SwappingStream ResolveStream(object content)
        {
            switch (content)
            {
                case Body body:
                {
                    ProtocolHolder.Headers.ContentType = body.ContentType;
                    body.Rewind();
                    return body.Stream;
                }
                case SwappingStream swappingStream: return swappingStream.Rewind();
                case Stream otherStream: return new SwappingStream(otherStream);
                case byte[] bytes: return new SwappingStream(bytes);
                case string str: return new SwappingStream(str.ToBytes());
                case null: return new SwappingStream();
            }

            var stream = new SwappingStream();
            var contentTypeProvider = ProtocolHolder.GetInputContentTypeProvider();
            switch (content)
            {
                case IDictionary<string, object> _:
                case JObject _:
                    contentTypeProvider.SerializeCollection(new[] {content}, stream);
                    break;
                case IEnumerable<object> ie:
                    contentTypeProvider.SerializeCollection(ie, stream);
                    break;
                case IEnumerable ie:
                    contentTypeProvider.SerializeCollection(ie.Cast<object>(), stream);
                    break;
                default:
                    contentTypeProvider.SerializeCollection(new[] {content}, stream);
                    break;
            }
            return stream.Rewind();
        }

        /// <summary>
        /// Only for outgoing streams
        /// </summary>
        private Body(IProtocolHolder protocolHolder)
        {
            ProtocolHolder = protocolHolder;
            IsIngoing = false;
            Stream = new SwappingStream();
        }

        internal static Body CreateOutputBody(IProtocolHolder protocolHolder)
        {
            return new(protocolHolder);
        }

        private const int MaxStringLength = 50_000;

        public string GetLengthLogString() => !HasContent ? "" : $" ({ContentLength} bytes)";

        /// <summary>
        /// Writes the body to a string
        /// </summary>
        /// <returns></returns>
        public async Task<string> ToStringAsync()
        {
            if (!HasContent) return "";
            Stream.Rewind();
            try
            {
                using var reader = new StreamReader(Stream, RESTableConfig.DefaultEncoding, false, 1024, true);
                if (Stream.Length > MaxStringLength)
                {
                    var buffer = new char[MaxStringLength];
                    await reader.ReadAsync(buffer, 0, buffer.Length);
                    return new string(buffer);
                }
                else
                {
                    return await reader.ReadToEndAsync();
                }
            }
            finally
            {
                Stream.Rewind();
            }
        }

        internal async Task<Body> GetCopy()
        {
            if (!HasContent) return default;
            var copy = new Body(ProtocolHolder);
            await Stream.CopyToAsync(copy.Stream);
            copy.Stream.Rewind();
            Stream.Rewind();
            return copy;
        }

        internal Body Rewind()
        {
            Stream.Rewind();
            return this;
        }

        // public override string ToString()
        // {
        //     // only do what can be done synchronously
        //     return base.ToString();
        // }

        public override async ValueTask DisposeAsync()
        {
            await Stream.DisposeAsync();
        }

        internal byte[] GetBytes() => Stream.GetBytes();
        internal Task<byte[]> GetBytesAsync() => Stream.GetBytesAsync();

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

        internal bool CanClose
        {
            get => Stream.CanClose;
            set => Stream.CanClose = value;
        }

        public override long Position
        {
            get => Stream.Position;
            set => Stream.Position = value;
        }

        public override object InitializeLifetimeService() => Stream.InitializeLifetimeService();
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state) => Stream.BeginRead(buffer, offset, count, callback, state);
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state) => Stream.BeginWrite(buffer, offset, count, callback, state);
        public override void CopyTo(Stream destination, int bufferSize) => Stream.CopyTo(destination, bufferSize);
        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) => Stream.CopyToAsync(destination, bufferSize, cancellationToken);
        public override int EndRead(IAsyncResult asyncResult) => Stream.EndRead(asyncResult);
        public override void EndWrite(IAsyncResult asyncResult) => Stream.EndWrite(asyncResult);
        public override Task FlushAsync(CancellationToken cancellationToken) => Stream.FlushAsync(cancellationToken);
        public override int Read(Span<byte> buffer) => Stream.Read(buffer);
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => Stream.ReadAsync(buffer, offset, count, cancellationToken);
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = new()) => Stream.ReadAsync(buffer, cancellationToken);
        public override int ReadByte() => Stream.ReadByte();
        public override void Write(ReadOnlySpan<byte> buffer) => Stream.Write(buffer);
        public override void WriteByte(byte value) => Stream.WriteByte(value);
        public override bool CanTimeout => Stream.CanTimeout;
        public override void Flush() => Stream.Flush();
        public override long Seek(long offset, SeekOrigin origin) => Stream.Seek(offset, origin);
        public override void SetLength(long value) => Stream.SetLength(value);
        public override int Read(byte[] buffer, int offset, int count) => Stream.Read(buffer, offset, count);
        public override bool CanRead => Stream.CanRead;
        public override bool CanSeek => Stream.CanSeek;
        public override bool CanWrite => Stream.CanWrite;
        public override long Length => Stream.Length;
        public override void Close() => Stream.Close();
        public override void Write(byte[] buffer, int offset, int count) => Stream.Write(buffer, offset, count);
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => Stream.WriteAsync(buffer, offset, count, cancellationToken);
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = new CancellationToken()) => Stream.WriteAsync(buffer, cancellationToken);

        #endregion
    }
}