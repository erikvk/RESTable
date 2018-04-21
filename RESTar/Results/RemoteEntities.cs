﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using RESTar.Internal;
using RESTar.Requests;

namespace RESTar.Results
{
    /// <inheritdoc cref="Content" />
    /// <inheritdoc cref="IEntities{T}" />
    /// <summary>
    /// An entity collection received from a remote RESTar service
    /// </summary>
    internal class RemoteEntities : Content, IEntities<JObject>
    {
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public ulong EntityCount { get; set; }
        public Type EntityType { get; }
        public bool IsPaged => EntityCount > 0 && (long) EntityCount == Request.MetaConditions.Limit;
        private IRequestInternal RequestInternal { get; }
        private IContentTypeProvider ContentTypeProvider { get; }

        /// <inheritdoc />
        public IUriComponents GetNextPageLink() => this.MakeNextPageLink(-1);

        /// <inheritdoc />
        public IUriComponents GetNextPageLink(int count) => this.MakeNextPageLink(count);

        public void SetContentDisposition(string extension)
        {
            Headers["Content-Disposition"] = $"attachment; filename={Request.Resource}_{DateTime.Now:yyMMddHHmmssfff}{extension}";
        }

        public override IEntities<T1> ToEntities<T1>() => new DeserializedTypeEnumerable<T1>(RequestInternal, Body, this, ContentTypeProvider);

        public IEnumerator<JObject> GetEnumerator() => new StreamEnumerator<JObject>(Body, ContentTypeProvider);

        internal RemoteEntities(IRequestInternal request, Stream jsonStream, ulong entityCount) : base(request)
        {
            RequestInternal = request;
            Body = jsonStream;
            EntityType = typeof(JObject);
            EntityCount = entityCount;
            IsSerialized = true;
            ContentTypeProvider = ContentTypeController.ResolveOutputContentTypeProvider(RequestInternal, null);
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

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public IEnumerator<T> GetEnumerator() => new StreamEnumerator<T>(Stream, ContentTypeProvider);

        #region Entities bindings

        public bool IsPaged => Entities.IsPaged;
        public IUriComponents GetNextPageLink(int count) => Entities.GetNextPageLink(count);
        public IUriComponents GetNextPageLink() => Entities.GetNextPageLink();
        public void SetContentDisposition(string extension) => Entities.SetContentDisposition(extension);
        public Type EntityType => Entities.EntityType;

        public ulong EntityCount
        {
            get => Entities.EntityCount;
            set => Entities.EntityCount = value;
        }

        #endregion
    }

    internal class StreamEnumerator<T> : IEnumerator<T> where T : class
    {
        private readonly IEnumerator<T> Enumerator;

        public StreamEnumerator(Stream stream, IContentTypeProvider contentTypeProvider)
        {
            var enumerable = contentTypeProvider.DeserializeCollection<T>(stream);
            Enumerator = enumerable.GetEnumerator();
        }

        public void Dispose() => Enumerator.Dispose();
        public bool MoveNext() => Enumerator.MoveNext();
        public void Reset() => Enumerator.Reset();
        object IEnumerator.Current => Current;
        public T Current => Enumerator.Current;
    }
}