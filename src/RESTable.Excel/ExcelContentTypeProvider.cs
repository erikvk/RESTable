﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using OfficeOpenXml;
using RESTable.ContentTypeProviders;
using RESTable.Json;
using RESTable.Meta;
using RESTable.Requests;

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

        /// <inheritdoc />
        public override async Task<long> SerializeCollection<T>(IAsyncEnumerable<T> collection, Stream stream, IRequest request, CancellationToken cancellationToken)
            where T : class
        {
            try
            {
                using var package = new ExcelPackage(stream);
                var currentRow = 1;
                var worksheet = package.Workbook.Worksheets.Add(request.Resource.Name);

                async Task writeEntities(IAsyncEnumerable<object> entities)
                {
                    switch (entities)
                    {
                        case IAsyncEnumerable<IDictionary<string, object>> dicts:
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
                        case IAsyncEnumerable<JObject> jobjects:
                            var _columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                            await foreach (var jobject in jobjects.ConfigureAwait(false))
                            {
                                currentRow += 1;
                                foreach (var (key, value) in jobject)
                                {
                                    if (!_columns.TryGetValue(key, out var column))
                                    {
                                        column = _columns.Count + 1;
                                        _columns[key] = column;
                                        var cell = worksheet.Cells[1, column];
                                        cell.Style.Font.Bold = true;
                                        cell.Value = key;
                                    }
                                    WriteExcelCell(worksheet.Cells[currentRow, column], value?.ToObject<object>());
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

        private static void WriteExcelCell(ExcelRange target, object? value)
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
                case JObject _:
                    target.Value = typeof(JObject).FullName;
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
        protected override async Task ProduceJsonArray(Stream excelStream, Stream jsonStream)
        {
            try
            {
                var swr = new StreamWriter(jsonStream, ExcelSettings.Encoding, 4096, true);
#if NETSTANDARD2_1
                await using (swr.ConfigureAwait(false))
#else
                using (swr)
#endif
                {
                    using var jwr = new RESTableFromExcelJsonTextWriter(swr);
                    using var package = new ExcelPackage(excelStream);

                    await jwr.WriteStartArrayAsync().ConfigureAwait(false);

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
                                await jwr.WriteStartObjectAsync().ConfigureAwait(false);
                                for (var col = 1; col <= columns; col += 1)
                                {
                                    if (propertyNames[col] is string propertyName)
                                    {
                                        await jwr.WritePropertyNameAsync(propertyName).ConfigureAwait(false);
                                        await jwr.WriteValueAsync(worksheet.Cells[row, col].Value).ConfigureAwait(false);
                                    }
                                }
                                await jwr.WriteEndObjectAsync().ConfigureAwait(false);
                            }
                        }
                    }

                    await jwr.WriteEndArrayAsync().ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                throw new ExcelInputException(e.Message);
            }
        }
    }
}