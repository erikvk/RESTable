using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
        private const string XMLMimeType = "application/xml";
        private const string RESTarSpecific = "application/restar-xml";
        private const string Brief = "xml";

        /// <inheritdoc />
        public string Name => "XML";

        /// <inheritdoc />
        public ContentType ContentType { get; } = new ContentType("application/xml; charset=utf-8");

        /// <inheritdoc />
        public string[] MatchStrings { get; set; } = {XMLMimeType, RESTarSpecific, Brief};

        /// <inheritdoc />
        public bool CanRead => false;

        /// <inheritdoc />
        public bool CanWrite => true;

        /// <inheritdoc />
        public string ContentDispositionFileExtension => ".xml";

        private static readonly JsonContentProvider JsonProvider;
        private static readonly byte[] XMLHeader;

        static XMLWriter()
        {
            JsonProvider = new JsonContentProvider();
            XMLHeader = Encoding.UTF8.GetBytes("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        }

        /// <inheritdoc />
        public Stream SerializeEntity<T>(T entity, IRequest request) where T : class
        {
            return XmlSerializeJsonStream(JsonProvider.SerializeEntity(entity, request));
        }

        /// <inheritdoc />
        public Stream SerializeCollection<T>(IEnumerable<T> entities, IRequest request, out ulong entityCount) where T : class
        {
            return XmlSerializeJsonStream(JsonProvider.SerializeCollection(entities, request, out entityCount));
        }

        private static Stream XmlSerializeJsonStream(Stream jsonStream)
        {
            using (var streamReader = new StreamReader(jsonStream))
            {
                var json = $"{{\"entity\":{streamReader.ReadToEnd()}}}";
                var xml = JsonConvert.DeserializeXmlNode(json, "root", true);
                var xmlStream = new MemoryStream();
                xmlStream.Write(XMLHeader, 0, XMLHeader.Length);
                xml.Save(xmlStream);
                xmlStream.Seek(0, SeekOrigin.Begin);
                return xmlStream;
            }
        }

        /// <inheritdoc />
        public T DeserializeEntity<T>(byte[] body) where T : class => throw new NotImplementedException();

        /// <inheritdoc />
        public List<T> DeserializeCollection<T>(byte[] body) where T : class => throw new NotImplementedException();

        /// <inheritdoc />
        public IEnumerable<T> Populate<T>(IEnumerable<T> entities, byte[] body) where T : class =>
            throw new NotImplementedException();
    }
}