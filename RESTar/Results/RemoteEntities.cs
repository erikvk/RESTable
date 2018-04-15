using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RESTar.ContentTypeProviders;
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

        /// <inheritdoc />
        public IUriComponents GetNextPageLink() => this.MakeNextPageLink(-1);

        /// <inheritdoc />
        public IUriComponents GetNextPageLink(int count) => this.MakeNextPageLink(count);

        public void SetContentDisposition(string extension)
        {
            Headers["Content-Disposition"] = $"attachment;filename={Request.Resource}_{DateTime.Now:yyMMddHHmmssfff}{extension}";
        }

        public override IEntities<T1> ToEntities<T1>()
        {
            Body.Seek(0, SeekOrigin.Begin);
            return new DeserialzedTypeEnumerable<T1>(Request, Body, this);
        }

        public IEnumerator<JObject> GetEnumerator()
        {
            Body.Seek(0, SeekOrigin.Begin);
            return new JsonStreamEnumerator<JObject>(Body);
        }

        public RemoteEntities(IRequest request, Stream jsonStream, ulong entityCount) : base(request)
        {
            Body = jsonStream;
            EntityType = typeof(JObject);
            EntityCount = entityCount;
            IsSerialized = true;
        }
    }

    internal class DeserialzedTypeEnumerable<T> : Content, IEntities<T> where T : class
    {
        private readonly Stream JsonStream;
        private readonly IEntities Entities;


        public DeserialzedTypeEnumerable(IRequest request, Stream jsonStream, IEntities entities) : base(request)
        {
            JsonStream = jsonStream;
            Entities = entities;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public IEnumerator<T> GetEnumerator() => new JsonStreamEnumerator<T>(JsonStream);

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

    internal class JsonStreamEnumerator<T> : IEnumerator<T> where T : class
    {
        private readonly Stream JsonStream;
        private readonly JsonReader JsonReader;

        public JsonStreamEnumerator(Stream jsonStream)
        {
            JsonStream = jsonStream;
            JsonReader = new JsonTextReader(new StreamReader(JsonStream, RESTarConfig.DefaultEncoding)) {CloseInput = true};
            JsonReader.Read();
        }

        private JsonStreamEnumerator(Stream jsonStream, JsonReader jsonReader)
        {
            JsonStream = jsonStream;
            JsonReader = jsonReader;
        }

        public void Dispose() => JsonReader.Close();

        public bool MoveNext()
        {
            switch (JsonReader.TokenType)
            {
                case JsonToken.None:
                    JsonReader.Read();
                    JsonReader.Read();
                    break;
                case JsonToken.EndObject:
                case JsonToken.StartArray:
                    JsonReader.Read();
                    if (JsonReader.TokenType == JsonToken.EndArray)
                        return false;
                    break;
                case var other: throw new JsonReaderException($"Unexpected JSON token: {other}. Expected object or array of objects.");
            }

            Current = JsonContentProvider.Serializer.Deserialize<T>(JsonReader);
            return true;
        }

        public void Reset()
        {
            JsonStream.Seek(0, SeekOrigin.Begin);
            Current = null;
        }

        object IEnumerator.Current => Current;

        public T Current { get; private set; }
    }
}