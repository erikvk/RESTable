using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RESTable.Admin;

#pragma warning disable 1591

namespace RESTable.ContentTypeProviders.NativeJsonProtocol
{
    public class RESTableJsonWriter : JsonTextWriter
    {
        private readonly string NewLine;
        private int BaseIndentation;
        private int CurrentDepth;
        public long ObjectsWritten { get; private set; }

        public override void WriteStartObject()
        {
            if (CurrentDepth == 0)
            {
                ObjectsWritten += 1;
                if (ObjectsWritten % 15000 == 0)
                    Flush();
            }
            CurrentDepth += 1;
            base.WriteStartObject();
        }

        public override async Task WriteStartObjectAsync(CancellationToken cancellationToken = new())
        {
            if (CurrentDepth == 0)
            {
                ObjectsWritten += 1;
                if (ObjectsWritten % 15000 == 0)
                    await FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            CurrentDepth += 1;
           await base.WriteStartObjectAsync(cancellationToken).ConfigureAwait(false);
        }

        public override void WriteEndObject()
        {
            CurrentDepth -= 1;
            base.WriteEndObject();
        }

        public async Task WriteIndentationAsync(CancellationToken cancellationToken = new())
        {
            await WriteIndentAsync(cancellationToken).ConfigureAwait(false);
        }
        
        public override Task WriteEndObjectAsync(CancellationToken cancellationToken = new())
        {
            CurrentDepth -= 1;
            return base.WriteEndObjectAsync(cancellationToken);
        }
        
        public RESTableJsonWriter(TextWriter textWriter, int baseIndentation) : base(textWriter)
        {
            BaseIndentation = baseIndentation;
            switch (Settings._LineEndings)
            {
                case LineEndings.Environment:
                    NewLine = Environment.NewLine;
                    break;
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

        protected override async Task WriteIndentAsync(CancellationToken cancellationToken)
        {
            await WriteWhitespaceAsync(NewLine, cancellationToken).ConfigureAwait(false);
            var currentIndentCount = Top * Indentation + BaseIndentation;
            for (var i = 0; i < currentIndentCount; i++)
                await WriteIndentSpaceAsync(cancellationToken).ConfigureAwait(false);
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