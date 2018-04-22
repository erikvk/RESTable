﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dynamit;
using Newtonsoft.Json.Linq;
using OfficeOpenXml;
using RESTar.Operations;
using RESTar.Reflection.Dynamic;
using RESTar.Serialization.NativeProtocol;
using Starcounter;

namespace RESTar.ContentTypeProviders
{
    /// <inheritdoc />
    public class ExcelContentProvider : JsonAdapterProvider
    {
        private const string ExcelMimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        private const string RESTarSpecific = "application/restar-excel";
        private const string Brief = "excel";

        /// <inheritdoc />
        public override string Name => "Microsoft Excel";

        /// <inheritdoc />
        public override ContentType ContentType { get; } = ExcelMimeType;

        /// <inheritdoc />
        public override string[] MatchStrings { get; set; } = {ExcelMimeType, RESTarSpecific, Brief};

        /// <inheritdoc />
        public override bool CanRead => true;

        /// <inheritdoc />
        public override bool CanWrite => true;

        /// <inheritdoc />
        public override string ContentDispositionFileExtension => ".xlsx";

        /// <inheritdoc />
        public override void SerializeCollection(IEnumerable<object> entities, Stream stream, IRequest request, out ulong entityCount)
        {
            if (entities == null)
            {
                entityCount = 0;
                return;
            }
            try
            {
                using (var package = new ExcelPackage(stream))
                {
                    var currentRow = 1;
                    var worksheet = package.Workbook.Worksheets.Add(request.Resource.Name);
                    worksheet.Cells.AutoFitColumns(0);
                    switch (entities)
                    {
                        case IEnumerable<IDictionary<string, object>> dicts:
                            var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                            foreach (var dict in dicts)
                            {
                                currentRow += 1;
                                foreach (var pair in dict)
                                {
                                    if (!columns.TryGetValue(pair.Key, out var column))
                                    {
                                        column = columns.Count + 1;
                                        columns[pair.Key] = column;
                                        worksheet.Cells[1, column].Value = pair.Key;
                                    }
                                    WriteExcelCell(worksheet.Cells[currentRow, column], pair.Value);
                                }
                            }
                            break;
                        case IEnumerable<JObject> jobjects:
                            var _columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                            foreach (var jobject in jobjects)
                            {
                                currentRow += 1;
                                foreach (var pair in jobject)
                                {
                                    if (!_columns.TryGetValue(pair.Key, out var column))
                                    {
                                        column = _columns.Count + 1;
                                        _columns[pair.Key] = column;
                                        worksheet.Cells[1, column].Value = pair.Key;
                                    }
                                    WriteExcelCell(worksheet.Cells[currentRow, column], pair.Value.ToObject<object>());
                                }
                            }
                            break;
                        default:
                            var properties = request.Resource.Members.Values.Where(p => !p.Hidden).ToList();
                            var columnIndex = 1;
                            foreach (var property in properties)
                            {
                                worksheet.Cells[1, columnIndex].Value = property.Name;
                                columnIndex += 1;
                            }
                            foreach (var entity in entities)
                            {
                                currentRow += 1;
                                columnIndex = 1;
                                foreach (var property in properties)
                                {
                                    WriteExcelCell(worksheet.Cells[currentRow, columnIndex], GetCellValue(property, entity));
                                    columnIndex += 1;
                                }
                            }
                            break;
                    }
                    if (currentRow == 1)
                    {
                        entityCount = 0UL;
                        return;
                    }
                    entityCount = (ulong) currentRow - 1;
                    package.Save();
                }
            }
            catch (Exception e)
            {
                throw new ExcelFormatError(e.Message, e);
            }
        }

        private static object GetCellValue(DeclaredProperty prop, object target)
        {
            switch (prop)
            {
                case var _ when prop.ExcelReducer != null: return prop.ExcelReducer((dynamic) target);
                case var _ when prop.Type.IsEnum: return prop.GetValue(target)?.ToString();
                default: return prop.GetValue(target);
            }
        }

        private static void WriteExcelCell(ExcelRange target, object value)
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
                case DDictionary _:
                    target.Value = $"$(ObjectNo: {value.GetObjectNo()})";
                    break;
                case IDictionary other:
                    target.Value = other.GetType().RESTarTypeName();
                    break;
                case IEnumerable<object> other:
                    target.Value = string.Join(", ", other.Select(o => o.ToString()));
                    break;
                case IEnumerable<DateTime> dateTimes:
                    target.Value = string.Join(", ", dateTimes.Select(o => o.ToString("O")));
                    break;
                case var valueTypeArray when value.GetType().Implements(typeof(IEnumerable<>), out var p) && p.Any() && p[0].IsValueType:
                    IEnumerable<object> objects = Enumerable.Cast<object>((dynamic) valueTypeArray);
                    target.Value = string.Join(", ", objects.Select(o => o.ToString()));
                    break;
                default:
                    target.Value = Do.Try(() => $"$(ObjectNo: {value.GetObjectNo()})", value.ToString);
                    break;
            }
        }

        /// <inheritdoc />
        protected override void ProduceJsonArray(Stream excelStream, Stream jsonStream)
        {
            try
            {
                using (var swr = new StreamWriter(jsonStream, UTF8, 1024, true))
                using (var jwr = new RESTarFromExcelJsonWriter(swr))
                using (var package = new ExcelPackage(excelStream))
                {
                    var inputTable = package.Workbook.Worksheets[1];
                    var (rows, columns ) = (inputTable.Dimension.Rows, inputTable.Dimension.Columns);
                    if (rows < 2) return;
                    var propertyNames = new string[columns + 1];
                    for (var col = 1; col <= columns; col += 1)
                        propertyNames[col] = inputTable.Cells[1, col].GetValue<string>();
                    jwr.WriteStartArray();
                    for (var row = 2; row <= rows; row += 1)
                    {
                        jwr.WriteStartObject();
                        for (var col = 1; col <= columns; col += 1)
                        {
                            if (propertyNames[col] is string propertyName)
                            {
                                jwr.WritePropertyName(propertyName);
                                jwr.WriteValue(inputTable.Cells[row, col].Value);
                            }
                        }
                        jwr.WriteEndObject();
                    }
                    jwr.WriteEndArray();
                }
            }
            catch (Exception e)
            {
                throw new ExcelInputError(e.Message);
            }
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an error when writing to Excel
    /// </summary>
    public class ExcelFormatError : Exception
    {
        internal ExcelFormatError(string message, Exception ie) : base($"RESTar was unable to write entities to excel. {message}. ", ie) { }
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an error when reading from Excel
    /// </summary>
    public class ExcelInputError : Exception
    {
        internal ExcelInputError(string message) : base(
            "There was a format error in the excel input. Check that the file is being transmitted properly. In " +
            "curl, make sure the flag '--data-binary' is used and not '--data' or '-d'. Message: " + message) { }
    }
}