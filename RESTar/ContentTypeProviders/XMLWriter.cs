using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace RESTar.ContentTypeProviders
{
    /// <inheritdoc />
    /// <summary>
    /// A simple XML writer implementation, enabling RESTar output to be 
    /// sent with the application/xml content type.
    /// </summary>
    public class XMLWriter : IContentTypeProvider
    {
        /// <inheritdoc />
        public ContentType[] CanWrite() => new ContentType[] {"application/xml"};

        /// <inheritdoc />
        public ContentType[] CanRead() => null;

        /// <inheritdoc />
        public string GetContentDispositionFileExtension(ContentType contentType) => ".xml";

        private readonly JsonContentProvider JsonProvider = new JsonContentProvider();

        /// <inheritdoc />
        public Stream SerializeEntity<T>(ContentType accept, T entity, IRequest request) where T : class
        {
            using (var stream = JsonProvider.SerializeEntity("application/json", entity, request))
            using (var streamReader = new StreamReader(stream))
            {
                var json = streamReader.ReadToEnd();
                var xml = JsonConvert.DeserializeXmlNode(json, "root", true);
                var xmlStream = new MemoryStream();
                xml.Save(xmlStream);
                return xmlStream;
            }
        }

        /// <inheritdoc />
        public Stream SerializeCollection<T>(ContentType accept, IEnumerable<T> entities, IRequest request, out ulong entityCount) where T : class
        {
            using (var stream = JsonProvider.SerializeCollection("application/json", entities, request, out entityCount))
            using (var streamReader = new StreamReader(stream))
            {
                var json = $"{{\"entity\":{streamReader.ReadToEnd()}}}";
                var xml = JsonConvert.DeserializeXmlNode(json, "root", true);
                var xmlStream = new MemoryStream();
                xml.Save(xmlStream);
                xmlStream.Seek(0, SeekOrigin.Begin);
                return xmlStream;
            }
        }

        /// <inheritdoc />
        public T DeserializeEntity<T>(ContentType contentType, byte[] body) where T : class => throw new NotImplementedException();

        /// <inheritdoc />
        public List<T> DeserializeCollection<T>(ContentType contentType, byte[] body) where T : class => throw new NotImplementedException();

        /// <inheritdoc />
        public IEnumerable<T> Populate<T>(ContentType contentType, IEnumerable<T> entities, byte[] body) where T : class =>
            throw new NotImplementedException();
    }
}