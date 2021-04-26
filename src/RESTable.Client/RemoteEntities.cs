using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RESTable.ContentTypeProviders;
using RESTable.Requests;
using RESTable.Results;

namespace RESTable.Client
{
    /// <inheritdoc cref="Content" />
    /// <inheritdoc cref="IEntities{T}" />
    /// <summary>
    /// An entity collection received from a remote RESTable service
    /// </summary>
    internal class RemoteEntities : Content, IEntities<JObject>
    {
        public Type EntityType { get; }
        private IContentTypeProvider ContentTypeProvider => Request.GetOutputContentTypeProvider();
        internal ISerializedResult SerializedResult { get; }

        public ValueTask<long> CountAsync() => throw new NotSupportedException();

        public override IEntities<T1> ToEntities<T1>() => new DeserializedTypeEnumerable<T1>(Request, SerializedResult.Body, this, ContentTypeProvider);

        public IAsyncEnumerator<JObject> GetAsyncEnumerator(CancellationToken cancellationToken = new())
        {
            cancellationToken.ThrowIfCancellationRequested();
            return new StreamEnumerator<JObject>(SerializedResult.Body, ContentTypeProvider);
        }

        internal RemoteEntities(IRequest request) : base(request)
        {
            EntityType = typeof(JObject);
            SerializedResult = new SerializedResult(this);
        }
    }
}