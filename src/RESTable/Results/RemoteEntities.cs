using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RESTable.ContentTypeProviders;
using RESTable.Internal;
using RESTable.Requests;

namespace RESTable.Results
{
    /// <inheritdoc cref="Content" />
    /// <inheritdoc cref="IEntities{T}" />
    /// <summary>
    /// An entity collection received from a remote RESTable service
    /// </summary>
    internal class RemoteEntities : Content, IEntities<JObject>
    {
        public Type EntityType { get; }
        private IRequestInternal RequestInternal { get; }
        private IContentTypeProvider ContentTypeProvider { get; }

        internal ISerializedResult SerializedResult { get; }

        public override IEntities<T1> ToEntities<T1>() => new DeserializedTypeEnumerable<T1>(RequestInternal, SerializedResult.Body, this, ContentTypeProvider);

        public IAsyncEnumerator<JObject> GetAsyncEnumerator(CancellationToken cancellationToken = new())
        {
            cancellationToken.ThrowIfCancellationRequested();
            return new StreamEnumerator<JObject>(SerializedResult.Body, ContentTypeProvider);
        }

        internal RemoteEntities(IRequestInternal request) : base(request)
        {
            RequestInternal = request;
            EntityType = typeof(JObject);
            ContentTypeProvider = RequestInternal.GetOutputContentTypeProvider();
            SerializedResult = new SerializedResult(this);
        }
    }

    internal class DeserializedTypeEnumerable<T> : Content, IEntities<T> where T : class
    {
        private readonly Stream Stream;
        private readonly IEntities Entities;
        private readonly IContentTypeProvider ContentTypeProvider;

        public DeserializedTypeEnumerable(IRequest request, Stream stream, IEntities entities, IContentTypeProvider contentTypeProvider) : base(request)
        {
            Stream = stream;
            Entities = entities;
            ContentTypeProvider = contentTypeProvider;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return new StreamEnumerator<T>(Stream, ContentTypeProvider);
        }

        #region Entities bindings

        public Type EntityType => Entities.EntityType;

        #endregion
    }

    internal class StreamEnumerator<T> : IAsyncEnumerator<T> where T : class
    {
        private readonly IAsyncEnumerator<T> Enumerator;

        public StreamEnumerator(Stream stream, IContentTypeProvider contentTypeProvider, CancellationToken cancellationToken = new())
        {
            var enumerable = contentTypeProvider.DeserializeCollection<T>(stream);
            Enumerator = enumerable.GetAsyncEnumerator(cancellationToken);
        }

        public async ValueTask DisposeAsync()
        {
            await Enumerator.DisposeAsync();
        }

        public ValueTask<bool> MoveNextAsync() => Enumerator.MoveNextAsync();

        public T Current => Enumerator.Current;
    }
}