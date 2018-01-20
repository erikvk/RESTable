using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using static RESTar.Admin.Settings;

#pragma warning disable 1591

namespace RESTar.Serialization
{
    public class C : JsonDictionaryContract
    {
        public C(Type underlyingType) : base(underlyingType) { }
    }

    public class ODataJsonWriter : JsonTextWriter
    {
        internal readonly string NewLine;
        private int BaseIndentation;
        private int CurrentDepth;
        public ulong ObjectsWritten { get; private set; }

        public override void WriteStartObject()
        {
            if (CurrentDepth == BaseIndentation)
                ObjectsWritten += 1;
            CurrentDepth += 1;
            base.WriteStartObject();
        }

        public override void WriteEndObject()
        {
            CurrentDepth -= 1;
            base.WriteEndObject();
        }

        public void WritePre()
        {
            WriteStartObject();
            WriteIndent();
        }

        public void WritePost()
        {
            WriteWhitespace(NewLine);
            WriteEndObject();
        }

        public void WriteIndentation()
        {
            WriteIndent();
        }

        public void WriteMetadata(string propertyName, long valueNr)
        {
            WriteIndent();
            WritePropertyName(propertyName);
            WriteWhitespace(" ");
            WriteValue(valueNr);
        }

        public ODataJsonWriter(TextWriter textWriter) : base(textWriter)
        {
            BaseIndentation = 1;
            switch (_LineEndings)
            {
                case LineEndings.Windows:
                    NewLine = "\r\n";
                    break;
                case LineEndings.Linux:
                    NewLine = "\n";
                    break;
                default: return;
            }
        }

        protected override void WriteIndent()
        {
            WriteWhitespace(NewLine);
            var currentIndentCount = Top * Indentation + BaseIndentation;
            for (var i = 0; i < currentIndentCount; i++)
                WriteIndentSpace();
        }

        protected override void Dispose(bool disposing)
        {
            CurrentDepth = 0;
            ObjectsWritten = 0;
            BaseIndentation = 0;
            base.Dispose(disposing);
        }
    }
}