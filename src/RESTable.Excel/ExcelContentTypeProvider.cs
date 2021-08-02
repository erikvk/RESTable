using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OfficeOpenXml;
using RESTable.ContentTypeProviders;
using RESTable.Json;
using RESTable.Meta;

namespace RESTable.Excel
{
    /// <inheritdoc cref="JsonAdapter" />
    /// <inheritdoc cref="IContentTypeProvider" />
    internal class ExcelContentTypeProvider : JsonAdapter, IContentTypeProvider
    {
        private const string ExcelMimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        private const string RESTableSpecific = "application/restable-excel";
        private const string Brief = "excel";

        /// <inheritdoc />
        public override string Name => "Microsoft Excel";

        /// <inheritdoc />
        public override ContentType ContentType => ExcelMimeType;

        /// <inheritdoc />
        public override string[] MatchStrings { get; set; } = {ExcelMimeType, RESTableSpecific, Brief};

        /// <inheritdoc />
        public override bool CanRead => true;

        /// <inheritdoc />
        public override bool CanWrite => true;

        /// <inheritdoc />
        public override string ContentDispositionFileExtension => ".xlsx";

        private TypeCache TypeCache { get; }
        private ExcelSettings ExcelSettings { get; }

        public ExcelContentTypeProvider(IJsonProvider jsonProvider, ExcelSettings excelSettings, TypeCache typeCache) : base(jsonProvider)
        {
            TypeCache = typeCache;
            ExcelSettings = excelSettings;
        }

        public override async Task SerializeAsync<T>(Stream stream, T item, CancellationToken cancellationToken)
        {
            await SerializeCollectionAsync(stream, Linq.Enumerable.ToAsyncSingleton(item), cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public override async ValueTask<long> SerializeCollectionAsync<T>(Stream stream, IAsyncEnumerable<T> collection, CancellationToken cancellationToken)
        {
            try
            {
                using var package = new ExcelPackage(stream);
                var currentRow = 1;
                var worksheet = package.Workbook.Worksheets.Add("Sheet 1");

                async Task writeEntities(IAsyncEnumerable<T> entities)
                {
                    switch (entities)
                    {
                        case IAsyncEnumerable<IDictionary<string, object?>> dicts:
                            var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                            await foreach (var dict in dicts.ConfigureAwait(false))
                            {
                                currentRow += 1;
                                foreach (var (key, value) in dict)
                                {
                                    if (!columns.TryGetValue(key, out var column))
                                    {
                                        column = columns.Count + 1;
                                        columns[key] = column;
                                        var cell = worksheet.Cells[1, column];
                                        cell.Style.Font.Bold = true;
                                        cell.Value = key;
                                    }
                                    WriteExcelCell(worksheet.Cells[currentRow, column], value);
                                }
                            }
                            break;
                        default:
                            var properties = TypeCache.GetDeclaredProperties(typeof(T)).Values.Where(p => !p.Hidden).ToList();
                            var columnIndex = 1;
                            foreach (var property in properties)
                            {
                                var cell = worksheet.Cells[1, columnIndex];
                                cell.Style.Font.Bold = true;
                                cell.Value = property.Name;
                                columnIndex += 1;
                            }
                            await foreach (var entity in entities.ConfigureAwait(false))
                            {
                                currentRow += 1;
                                columnIndex = 1;
                                foreach (var property in properties)
                                {
                                    WriteExcelCell(worksheet.Cells[currentRow, columnIndex], await GetCellValue(property, entity).ConfigureAwait(false));
                                    columnIndex += 1;
                                }
                            }
                            break;
                    }
                }

                await writeEntities(collection).ConfigureAwait(false);
                if (currentRow == 1) return 0;
                worksheet.Cells.AutoFitColumns(0);
                package.Save();
                return (long) currentRow - 1;
            }
            catch (Exception e)
            {
                throw new ExcelFormatException(e.Message, e);
            }
        }

        private static async ValueTask<object?> GetCellValue(DeclaredProperty prop, object target) => prop switch
        {
            _ when prop.ExcelReducer is not null => ((dynamic) prop.ExcelReducer)(target),
            _ when prop.Type.IsEnum => (await prop.GetValue(target).ConfigureAwait(false))?.ToString(),
            _ => await prop.GetValue(target).ConfigureAwait(false)
        };

        private void WriteExcelCell(ExcelRange target, object? value)
        {
            switch (value)
            {
                case null:
                case DBNull _:
                case bool _:
                case decimal _:
                case long _:
                case sbyte _:
                case byte _:
                case short _:
                case ushort _:
                case int _:
                case uint _:
                case ulong _:
                case float _:
                case double _:
                case string _:
                    target.Value = value;
                    break;
                case DateTime dt:
                    target.Style.Numberformat.Format = "mm-dd-yy";
                    target.Value = dt;
                    break;
                case char @char:
                    target.Value = @char.ToString();
                    break;
                case JsonElement {ValueKind: JsonValueKind.Array} jarr:
                    target.Value = string.Join(", ", jarr.EnumerateArray().Select(o => JsonProvider.ToObject<object>(o)?.ToString()));
                    break;
                case JsonElement {ValueKind: JsonValueKind.Object}:
                    target.Value = typeof(JsonElement).FullName;
                    break;
                case JsonElement element:
                    target.Value = JsonProvider.ToObject<object>(element);
                    break;
                case IDictionary other:
                    target.Value = other.GetType().GetRESTableTypeName();
                    break;
                case IEnumerable<object> other:
                    target.Value = string.Join(", ", other.Select(o => o.ToString()));
                    break;
                case IEnumerable<DateTime> dateTimes:
                    target.Value = string.Join(", ", dateTimes.Select(o => o.ToString("O")));
                    break;
                case var valArr when value.GetType().ImplementsGenericInterface(typeof(IEnumerable<>), out var p) && p!.Any() && p![0].IsValueType:
                    IEnumerable<object> objects = Enumerable.Cast<object>((dynamic) valArr);
                    target.Value = string.Join(", ", objects.Select(o => o.ToString()));
                    break;
            }
        }

        /// <inheritdoc />
        protected override async Task ProduceJsonArrayAsync(Stream excelStream, Stream jsonStream)
        {
            try
            {
                await using var jwr = new Utf8JsonWriter(jsonStream);
                using var package = new ExcelPackage(excelStream);

                jwr.WriteStartArray();

                var worksheet = package.Workbook?.Worksheets?.FirstOrDefault();
                if (worksheet?.Dimension is not null)
                {
                    var (rows, columns) = (worksheet.Dimension.Rows, worksheet.Dimension.Columns);
                    if (rows > 1)
                    {
                        var propertyNames = new string[columns + 1];
                        for (var col = 1; col <= columns; col += 1)
                            propertyNames[col] = worksheet.Cells[1, col].GetValue<string>();
                        for (var row = 2; row <= rows; row += 1)
                        {
                            jwr.WriteStartObject();
                            for (var col = 1; col <= columns; col += 1)
                            {
                                if (propertyNames[col] is string propertyName)
                                {
                                    jwr.WritePropertyName(propertyName);
                                    switch (worksheet.Cells[row, col].Value)
                                    {
                                        case null:
                                            jwr.WriteNullValue();
                                            break;
                                        case string str:
                                            jwr.WriteStringValue(str);
                                            break;
                                        case char ch:
                                            jwr.WriteStringValue(ch.ToString());
                                            break;
                                        case bool b:
                                            jwr.WriteBooleanValue(b);
                                            break;
                                        case decimal d:
                                            jwr.WriteNumberValue(d);
                                            break;
                                        case long l:
                                            jwr.WriteNumberValue(l);
                                            break;
                                        case sbyte s:
                                            jwr.WriteNumberValue(s);
                                            break;
                                        case byte b:
                                            jwr.WriteNumberValue(b);
                                            break;
                                        case short sh:
                                            jwr.WriteNumberValue(sh);
                                            break;
                                        case ushort us:
                                            jwr.WriteNumberValue(us);
                                            break;
                                        case int i:
                                            jwr.WriteNumberValue(i);
                                            break;
                                        case uint ui:
                                            jwr.WriteNumberValue(ui);
                                            break;
                                        case ulong ul:
                                            jwr.WriteNumberValue(ul);
                                            break;
                                        case float f:
                                            jwr.WriteNumberValue(f);
                                            break;
                                        case double de:
                                            jwr.WriteNumberValue(de);
                                            break;
                                        case DateTime dt:
                                            jwr.WriteStringValue(dt.ToString("O"));
                                            break;
                                    }
                                }
                            }
                            jwr.WriteEndObject();
                        }
                    }
                }
                jwr.WriteEndArray();
            }
            catch (Exception e)
            {
                throw new ExcelInputException(e.Message);
            }
        }
    }
}