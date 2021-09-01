#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OfficeOpenXml;
using RESTable.ContentTypeProviders;
using RESTable.Meta;

namespace RESTable.Excel
{
    /// <inheritdoc cref="IContentTypeProvider" />
    internal class ExcelContentTypeProvider : IContentTypeProvider
    {
        private const string ExcelMimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        private const string RESTableSpecific = "application/restable-excel";
        private const string Brief = "excel";

        private ExcelSettings ExcelSettings { get; }
        private ISerializationMetadataAccessor MetadataAccessor { get; }
        private IJsonProvider JsonProvider { get; }
        private TypeCache TypeCache { get; }

        public string Name => "Microsoft Excel";
        public ContentType ContentType => ExcelMimeType;
        public string[] MatchStrings => new[] { ExcelMimeType, RESTableSpecific, Brief };
        public bool CanRead => true;
        public bool CanWrite => true;
        public string ContentDispositionFileExtension => ".xlsx";

        public ExcelContentTypeProvider(ExcelSettings excelSettings, ISerializationMetadataAccessor metadataAccessor, IJsonProvider jsonProvider, TypeCache typeCache)
        {
            MetadataAccessor = metadataAccessor;
            JsonProvider = jsonProvider;
            TypeCache = typeCache;
            ExcelSettings = excelSettings;
        }

        public async Task SerializeAsync<T>(Stream stream, T item, CancellationToken cancellationToken)
        {
            await SerializeAsyncEnumerable(stream, Linq.Enumerable.ToAsyncSingleton(item), cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async ValueTask<long> SerializeAsyncEnumerable<T>(Stream stream, IAsyncEnumerable<T> collection, CancellationToken cancellationToken)
        {
            try
            {
                await using var localStream = new SwappingStream();
                using var package = new ExcelPackage(localStream);
                var metadata = MetadataAccessor.GetMetadata<T>();
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
                            var properties = metadata.PropertiesToSerialize;
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
                                    WriteExcelCell(worksheet.Cells[currentRow, columnIndex], await GetCellValue(property, entity!).ConfigureAwait(false));
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
                localStream.Seek(0, SeekOrigin.Begin);
                await localStream.CopyToAsync(stream, 81920, cancellationToken).ConfigureAwait(false);
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

        public async IAsyncEnumerable<T> DeserializeAsyncEnumerable<T>(Stream stream, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var localStream = new SwappingStream();
            await stream.CopyToAsync(localStream, 81920, cancellationToken).ConfigureAwait(false);
            using var package = new ExcelPackage(localStream);
            var worksheet = package.Workbook?.Worksheets?.FirstOrDefault();
            if (worksheet?.Dimension is null)
            {
                yield break;
            }
            var (rowCount, columnCount) = (worksheet.Dimension.Rows, worksheet.Dimension.Columns);
            if (rowCount <= 1)
            {
                // We need at least two rows, the first containing property names, and the second values
                yield break;
            }
            var metadata = MetadataAccessor.GetMetadata<T>();

            // Get the properties of the referenced entities, as defined in the first row
            var referencedProperties = GetReferencedProperties(worksheet, rowNumber: 1, columnCount, metadata);

            for (var row = 2; row <= rowCount; row += 1)
            {
                var instance = metadata.InvokeParameterlessConstructor();
                for (var i = 0; i < referencedProperties.Length; i += 1)
                {
                    var (property, columnIndex) = referencedProperties[i];
                    var value = worksheet.Cells[row, columnIndex].Value;
                    await property.SetValue(instance!, value).ConfigureAwait(false);
                }
                yield return instance;
            }
        }

        private static (Property property, int columnIndex)[] GetReferencedProperties<T>
        (
            ExcelWorksheet worksheet,
            int rowNumber,
            int columnCount,
            ISerializationMetadata<T> metadata
        )
        {
            var referencedPropertiesList = new List<(Property property, int columnIndex)>(columnCount + 1);
            for (var columnIndex = 1; columnIndex <= columnCount; columnIndex += 1)
            {
                // Read property names from the first row
                var propertyName = worksheet.Cells[rowNumber, columnIndex].GetValue<string>();
                if (metadata.GetProperty(propertyName) is { IsWritable: true } writeable)
                    referencedPropertiesList.Add((writeable, columnIndex));
                else if (metadata.TypeIsDictionary)
                    referencedPropertiesList.Add((DynamicProperty.Parse(propertyName), columnIndex));
            }
            return referencedPropertiesList.ToArray();
        }

        private static PopulateSource GetPopulateSource(object?[] excelRow, (Property property, int columnIndex)[] referencedProperties)
        {
            var properties = referencedProperties.Select(pair =>
            {
                var (property, columnIndex) = pair;
                var name = property.Name;
                var populateSource = new PopulateSource(SourceKind.Value, new ExcelValueProvider(excelRow[columnIndex]));
                return (name, populateSource);
            }).ToArray();
            var valueResolver = new ExcelValueProvider(excelRow);
            return new PopulateSource(SourceKind.Object, valueResolver, properties);
        }

        public IAsyncEnumerable<T> Populate<T>(IAsyncEnumerable<T> entities, Stream stream, CancellationToken cancellationToken) where T : notnull
        {
            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook?.Worksheets?.FirstOrDefault();
            if (worksheet?.Dimension is null)
            {
                return entities;
            }
            var (rowCount, columnCount) = (worksheet.Dimension.Rows, worksheet.Dimension.Columns);
            if (rowCount <= 1)
            {
                // We need at least two rows, the first containing property names, and the second values
                return entities;
            }
            var referencedProperties = GetReferencedProperties(worksheet, 1, columnCount, MetadataAccessor.GetMetadata<T>());
            var valueRow = referencedProperties.Select(property => worksheet.Cells[2, property.columnIndex].Value).ToArray();
            var populateSource = GetPopulateSource(valueRow, referencedProperties);
            var populator = new Populator(typeof(T), populateSource, TypeCache);
            return PopulateInternal(entities, populator, cancellationToken);
        }

        private static async IAsyncEnumerable<T> PopulateInternal<T>
        (
            IAsyncEnumerable<T> entities,
            Populator populator,
            [EnumeratorCancellation] CancellationToken cancellationToken
        )
            where T : notnull
        {
            await foreach (var item in entities.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                yield return (T) await populator.PopulateAsync(item).ConfigureAwait(false);
            }
        }

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
                case Enum e:
                    target.Value = e.ToString();
                    break;
                case DateTime dt:
                    target.Style.Numberformat.Format = "mm-dd-yy";
                    target.Value = dt;
                    break;
                case char @char:
                    target.Value = @char.ToString();
                    break;
                case JsonElement { ValueKind: JsonValueKind.Array } jarr:
                    target.Value = string.Join(", ", jarr.EnumerateArray().Select(o => JsonProvider.ToObject<object>(o)?.ToString()));
                    break;
                case JsonElement { ValueKind: JsonValueKind.Object }:
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
    }
}