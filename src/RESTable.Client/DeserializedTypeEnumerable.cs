using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using RESTable.ContentTypeProviders;
using RESTable.Requests;
using RESTable.Results;

namespace RESTable.Client
{
    internal class DeserializedTypeEnumerable<T> : Content, IEntities<T> where T : class
    {
        private readonly Stream Stream;
        private readonly IEntities Entities;
        private readonly IContentTypeProvider ContentTypeProvider;

        public ValueTask<long> CountAsync() => Entities.CountAsync();

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
}