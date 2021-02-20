using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RESTable.Requests;

namespace RESTable.ContentTypeProviders
{
    /// <inheritdoc />
    /// <summary>
    /// A simple XML writer implementation, enabling RESTable output to be 
    /// sent with the application/xml content type.
    /// </summary>
    public class XMLWriter : IContentTypeProvider
    {
        private const string XMLMimeType = "application/xml";
        private const string RESTableSpecific = "application/restable-xml";
        private const string Brief = "xml";

        /// <inheritdoc />
        public string Name => "XML";

        /// <inheritdoc />
        public ContentType ContentType { get; } = "application/xml; charset=utf-8";

        /// <inheritdoc />
        public string[] MatchStrings { get; set; } = {XMLMimeType, RESTableSpecific, Brief};

        /// <inheritdoc />
        public bool CanRead => false;

        /// <inheritdoc />
        public bool CanWrite => true;

        /// <inheritdoc />
        public string ContentDispositionFileExtension => ".xml";

        private static readonly NewtonsoftJsonProvider JsonProvider;
        private static readonly byte[] XMLHeader;

        static XMLWriter()
        {
            JsonProvider = new NewtonsoftJsonProvider();
            XMLHeader = Encoding.UTF8.GetBytes("<?xml version=\"1.0\" encoding=\"utf-8\"?>\n");
        }

        /// <inheritdoc />
        public async Task<long> SerializeCollection<T>(IAsyncEnumerable<T> collection, Stream stream, IRequest request = null) where T : class
        {
            var count = await JsonProvider.SerializeCollection(collection, stream, request).ConfigureAwait(false);
            stream.Seek(0, SeekOrigin.Begin);
            await XmlSerializeJsonStream(stream).ConfigureAwait(false);
            return count;
        }

        private static async Task XmlSerializeJsonStream(Stream stream)
        {
            using var streamReader = new StreamReader(stream, RESTableConfig.DefaultEncoding, false, 1024, true);
            var json = $"{{\"entity\":{await streamReader.ReadToEndAsync().ConfigureAwait(false)}}}";
            var xml = JsonConvert.DeserializeXmlNode(json, "root", true);
            stream.Seek(0, SeekOrigin.Begin);
            await stream.WriteAsync(XMLHeader, 0, XMLHeader.Length).ConfigureAwait(false);
            xml?.Save(stream);
        }

        /// <inheritdoc />
        public IAsyncEnumerable<T> DeserializeCollection<T>(Stream body) => throw new NotImplementedException();

        /// <inheritdoc />
        public IAsyncEnumerable<T> Populate<T>(IAsyncEnumerable<T> entities, byte[] body) => throw new NotImplementedException();
    }
}