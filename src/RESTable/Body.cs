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
using RESTable.Linq;
using RESTable.Resources.Operations;
using RESTable.Results;

namespace RESTable
{
    /// <inheritdoc cref="IDisposable" />
    /// <summary>
    /// Encodes a request body
    /// </summary>
    public class Body : Stream, IDisposable, IAsyncDisposable
    {
        [NotNull] private SwappingStream Stream { get; }

        private IProtocolHolder ProtocolHolder { get; }

        private bool IsIngoing { get; }

        private object UninitializedBodyObject { get; set; }

        public ContentType ContentType => IsIngoing
            ? ProtocolHolder.GetInputContentTypeProvider().ContentType
            : ProtocolHolder.GetOutputContentTypeProvider().ContentType;

        private IContentTypeProvider ContentTypeProvider => IsIngoing
            ? ProtocolHolder.GetInputContentTypeProvider()
            : ProtocolHolder.GetOutputContentTypeProvider();

        /// <summary>
        /// Serializes the given result object to this body, using the appropriate content type
        /// provider assigned to the request or response that this body belongs to.
        /// </summary>
        /// <param name="result"></param>
        public async Task Serialize(ISerializedResult result, CancellationToken cancellationToken)
        {
            await ProtocolHolder.CachedProtocolProvider.ProtocolProvider.SerializeResult(result, ContentTypeProvider, cancellationToken).ConfigureAwait(false);
            TryRewind();
        }

        /// <summary>
        /// Deserializes the body to an IEnumerable of entities of the given type
        /// </summary>
        public IAsyncEnumerable<T> Deserialize<T>()
        {
            if (!HasContent) return null;
            try
            {
                return ContentTypeProvider.DeserializeCollection<T>(Stream);
            }
            finally
            {
                if (CanSeek)
                    Stream.Rewind();
            }
        }

        /// <summary>   
        /// Populates the body onto each entity in a source collection. If the body is empty,
        /// returns null.
        /// </summary>
        public IAsyncEnumerable<T> PopulateTo<T>(IAsyncEnumerable<T> source) where T : class
        {
            if (source == null || !HasContent) return null;
            try
            {
                return ContentTypeProvider.Populate(source, Stream.GetBytes());
            }
            finally
            {
                if (CanSeek)
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
            _ => true
        };

        /// <summary>
        /// The length of the body content in bytes
        /// /// </summary>
        public long? ContentLength => Do.SafeGet<long?>(() => Stream.Length);

        public Task MakeSeekable() => Stream.MakeSeekable();

        public Body(IProtocolHolder protocolHolder, object bodyObject = null)
        {
            ProtocolHolder = protocolHolder;
            IsIngoing = true;
            Stream = ResolveStream(bodyObject);
        }

        private SwappingStream ResolveStream(object bodyObject)
        {
            switch (bodyObject)
            {
                case Body body:
                {
                    ProtocolHolder.Headers.ContentType = body.ContentType;
                    body.TryRewind();
                    return body.Stream;
                }
                case SwappingStream swappingStream: return swappingStream.Rewind();
                case Stream otherStream: return new SwappingStream(otherStream);
                case byte[] bytes: return new SwappingStream(bytes);
                case string str: return new SwappingStream(str.ToBytes());
                case null: return new SwappingStream();
                default:
                    UninitializedBodyObject = bodyObject;
                    return new SwappingStream();
            }
        }

        public async Task Initialize(CancellationToken cancellationToken)
        {
            if (UninitializedBodyObject == null)
                return;
            var contentTypeProvider = ProtocolHolder.GetInputContentTypeProvider();
            var content = UninitializedBodyObject;
            switch (content)
            {
                case IDictionary<string, object> _:
                case JObject _:
                    await contentTypeProvider.SerializeCollection(content.ToAsyncSingleton(), Stream, null, cancellationToken).ConfigureAwait(false);
                    break;
                case IAsyncEnumerable<object> aie:
                    await contentTypeProvider.SerializeCollection(aie, Stream, null, cancellationToken).ConfigureAwait(false);
                    break;
                case IEnumerable<object> ie:
                    await contentTypeProvider.SerializeCollection(ie.ToAsyncEnumerable(), Stream, null, cancellationToken).ConfigureAwait(false);
                    break;
                case IEnumerable ie:
                    await contentTypeProvider.SerializeCollection(ie.Cast<object>().ToAsyncEnumerable(), Stream, null, cancellationToken).ConfigureAwait(false);
                    break;
                default:
                    await contentTypeProvider.SerializeCollection(content.ToAsyncSingleton(), Stream, null, cancellationToken).ConfigureAwait(false);
                    break;
            }
            Stream.Rewind();
            UninitializedBodyObject = null;
        }

        /// <summary>
        /// Only for outgoing streams
        /// </summary>
        private Body(IProtocolHolder protocolHolder, Stream customOutputStream)
        {
            ProtocolHolder = protocolHolder;
            IsIngoing = false;
            Stream = new SwappingStream(customOutputStream);
        }

        internal static Body CreateOutputBody(IProtocolHolder protocolHolder, Stream customOutputStream)
        {
            return new(protocolHolder, customOutputStream);
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
                    await reader.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                    return new string(buffer);
                }
                else
                {
                    return await reader.ReadToEndAsync().ConfigureAwait(false);
                }
            }
            finally
            {
                Stream.Rewind();
            }
        }

        public async Task<Body> GetCopy()
        {
            if (!HasContent) return default;
            var copy = new Body(ProtocolHolder);
            await Stream.CopyToAsync(copy.Stream).ConfigureAwait(false);
            copy.Stream.Rewind();
            Stream.Rewind();
            return copy;
        }

        internal bool TryRewind()
        {
            if (!Stream.CanSeek) 
                return false;
            Stream.Rewind();
            return true;
        }

        // public override string ToString()
        // {
        //     // only do what can be done synchronously
        //     return base.ToString();
        // }

        protected override void Dispose(bool disposing)
        {
            Stream.Dispose();
            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            await Stream.DisposeAsync().ConfigureAwait(false);
            await base.DisposeAsync().ConfigureAwait(false);
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