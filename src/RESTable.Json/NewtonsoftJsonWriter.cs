using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using RESTable.ContentTypeProviders;

#pragma warning disable 1591

namespace RESTable.Json
{
    public class SystemTextJsonWriter : IJsonWriter
    {
        private readonly string NewLine;
        private int BaseIndentation;
        private int CurrentDepth;
        private bool CountObjectsWritten { get; set; }
        private long ObjectsWritten { get; set; }
        private int LevelToCountObjectsAt { get; set; }

        public void StartCountObjectsWritten()
        {
            CountObjectsWritten = true;
            LevelToCountObjectsAt = CurrentDepth;
            ObjectsWritten = 0;
        }

        public long StopCountObjectsWritten()
        {
            CountObjectsWritten = false;
            return ObjectsWritten;
        }

        public void WriteStartObject()
        {
            if (CurrentDepth == LevelToCountObjectsAt)
            {
                ObjectsWritten += 1;
                if (ObjectsWritten % 15000 == 0)
                    Flush();
            }
            CurrentDepth += 1;
            base.WriteStartObject();
        }

        public async Task WriteStartObjectAsync(CancellationToken cancellationToken = new())
        {
            if (CurrentDepth == LevelToCountObjectsAt)
            {
                ObjectsWritten += 1;
                if (ObjectsWritten % 15000 == 0)
                    await FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            CurrentDepth += 1;
            await base.WriteStartObjectAsync(cancellationToken).ConfigureAwait(false);
        }

        public void WriteEndObject()
        {
            CurrentDepth -= 1;
            base.WriteEndObject();
            if (CurrentDepth == LevelToCountObjectsAt)
            {
                Flush();
            }
        }

        public async Task WriteEndObjectAsync(CancellationToken cancellationToken = new())
        {
            CurrentDepth -= 1;
            await base.WriteEndObjectAsync(cancellationToken).ConfigureAwait(false);
            if (CurrentDepth == LevelToCountObjectsAt)
            {
                await FlushAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public SystemTextJsonWriter(TextWriter textWriter, LineEndings lineEndings, int baseIndentation)
        {
            BaseIndentation = baseIndentation;
            NewLine = lineEndings switch
            {
                LineEndings.Environment => Environment.NewLine,
                LineEndings.Windows => "\r\n",
                _ => "\n"
            };
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