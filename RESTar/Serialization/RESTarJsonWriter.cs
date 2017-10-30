using System.IO;
using Newtonsoft.Json;
using static RESTar.Admin.Settings;

#pragma warning disable 1591

namespace RESTar.Serialization
{
    public class RESTarJsonWriter : JsonTextWriter
    {
        private readonly string newLine;

        public RESTarJsonWriter(TextWriter textWriter) : base(textWriter)
        {
            switch (_LineEndings)
            {
                case LineEndings.Windows:
                    newLine = "\r\n";
                    break;
                case LineEndings.Linux:
                    newLine = "\n";
                    break;
                default: return;
            }
        }
        
        protected override void WriteIndent()
        {
            if (Formatting != Formatting.Indented) return;
            WriteWhitespace(newLine);
            var currentIndentCount = Top * Indentation;
            for (var i = 0; i < currentIndentCount; i++)
                WriteIndentSpace();
        }
    }
}