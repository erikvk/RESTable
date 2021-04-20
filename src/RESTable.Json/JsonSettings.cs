using System.Text;

namespace RESTable.Json
{
    public class JsonSettings
    {
        public LineEndings LineEndings { get; set; }
        public bool PrettyPrint { get; set; }
        public Encoding Encoding { get; set; }

        public JsonSettings()
        {
            LineEndings = LineEndings.Environment;
            PrettyPrint = true;
            Encoding = new UTF8Encoding(false);
        }
    }
}