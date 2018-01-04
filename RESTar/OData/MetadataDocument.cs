using System.IO;
using System.Xml;
using RESTar.Results.Success;

namespace RESTar.OData
{
    internal class MetadataDocument : OK
    {
        internal MetadataDocument()
        {
            ContentType = "application/xml";
            Body = new MemoryStream();
            using (var swr = new StreamWriter(Body, Serialization.Serializer.UTF8, 1024, true))
            using (var xwr = XmlWriter.Create(swr))
            {
                WritePre(xwr);

                WritePost(xwr);
            }
        }

        private static void WritePre(XmlWriter writer)
        {
            writer.WriteRaw("<edmx:Edmx Version=\"4.0\" xmlns:edmx=\"http://docs.oasis-open.org/odata/ns/edmx\"><edmx:DataServices>");
        }

        private static void WritePost(XmlWriter writer)
        {
            writer.WriteRaw("</edmx:DataServices></edmx:Edmx>");
        }
    }
}