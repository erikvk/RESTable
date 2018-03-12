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
            XMLHeader = Encoding.UTF8.GetBytes("<?xml version=\"1.0\" encoding=\"utf-8\"?>\n");
        }

        /// <inheritdoc />
        public void SerializeEntity<T>(T entity, Stream stream, IRequest request, out ulong entityCount) where T : class
        {
            JsonProvider.SerializeEntity(entity, stream, request, out entityCount);
            XmlSerializeJsonStream(stream);
        }

        /// <inheritdoc />
        public void SerializeCollection<T>(IEnumerable<T> entities, Stream stream, IRequest request, out ulong entityCount) where T : class
        {
            JsonProvider.SerializeCollection(entities, stream, request, out entityCount);
            XmlSerializeJsonStream(stream);
        }

        private static void XmlSerializeJsonStream(Stream stream)
        {
            using (var streamReader = new StreamReader(stream, RESTarConfig.DefaultEncoding, false, 1024, true))
            {
                var json = $"{{\"entity\":{streamReader.ReadToEnd()}}}";
                var xml = JsonConvert.DeserializeXmlNode(json, "root", true);
                stream.Seek(0, SeekOrigin.Begin);
                stream.Write(XMLHeader, 0, XMLHeader.Length);
                xml.Save(stream);
                stream.Seek(0, SeekOrigin.Begin);
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