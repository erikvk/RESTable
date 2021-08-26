using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using RESTable.Resources;

namespace RESTable.SQLite
{
    [RESTable]
    public class SQLiteShell : Terminal
    {
        public override async Task HandleTextInput(string input, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(input))
                return;
            if (string.Equals(input, "tables", StringComparison.OrdinalIgnoreCase))
                input = "SELECT name FROM sqlite_master WHERE type ='table' AND name NOT LIKE 'sqlite_%';";
            var query = new Query(input);
            await using var outputStream = await WebSocket
                .GetMessageStream(asText: true, cancellationToken)
                .ConfigureAwait(false);
            await using var responseWriter = new StreamWriter(outputStream);
            var first = true;
            await foreach (var row in query.GetRows(cancellationToken).ConfigureAwait(false))
            {
                if (!first)
                {
                    await responseWriter.WriteLineAsync().ConfigureAwait(false);
                }
                first = false;

                IEnumerable<object> getRowCells()
                {
                    for (var i = 0; i < row.FieldCount; i += 1)
                    {
                        yield return row.GetValue(i);
                    }
                }

                var rowData = string.Join(" | ", getRowCells()).AsMemory();
                await responseWriter.WriteAsync(rowData, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}