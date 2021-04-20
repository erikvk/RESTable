using System.Text;

namespace RESTable.Xml
{
    public class XmlSettings
    {
        public Encoding Encoding { get; set; }

        public XmlSettings()
        {
            Encoding = new UTF8Encoding(false);
        }
    }
}