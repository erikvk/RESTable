using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ExcelDataReader;
using RESTar.Internal;
using RESTar.Results.Error.BadRequest;
using RESTar.Serialization.NativeProtocol;
using static System.Linq.Enumerable;

namespace RESTar.ContentTypeProviders
{
    /// <inheritdoc />
    public class ExcelContentProvider : JsonAdapterProvider
    {
        private const string ExcelMimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        private const string RESTarSpecific = "application/restar-excel";
        private const string Brief = "excel";

        /// <inheritdoc />
        public override ContentType ContentType { get; } = new ContentType(ExcelMimeType);

        /// <inheritdoc />
        public override string[] MatchStrings => new[] {ExcelMimeType, RESTarSpecific, Brief};

        /// <inheritdoc />
        public override bool CanRead => true;

        /// <inheritdoc />
        public override bool CanWrite => true;

        /// <inheritdoc />
        public override string ContentDispositionFileExtension => ".xlsx";

        /// <inheritdoc />
        public override Stream SerializeEntity<T>(T entity, IRequest request) => SerializeCollection(new[] {entity}, request, out var _);

        /// <inheritdoc />
        public override Stream SerializeCollection<T>(IEnumerable<T> entities, IRequest request, out ulong entityCount)
        {
            try
            {
                entityCount = 0;
                var excel = entities.ToExcel(request.Resource);
                entityCount = (ulong) (excel?.Worksheet(1)?.RowsUsed().Count() - 1 ?? 0L);
                if (excel == null || entityCount == 0)
                    return null;
                var stream = new MemoryStream();
                excel.SaveAs(stream);
                stream.Seek(0, SeekOrigin.Begin);
                return stream;
            }
            catch (Exception e)
            {
                throw new ExcelFormatError(e.Message, e);
            }
        }

        /// <inheritdoc />
        protected override byte[] ProduceJson(byte[] excelBytes, out bool singular)
        {
            try
            {
                var jsonStream = new MemoryStream();
                using (var excelStream = new MemoryStream(excelBytes))
                using (var swr = new StreamWriter(jsonStream, UTF8, 1024, true))
                using (var jwr = new RESTarFromExcelJsonWriter(swr))
                using (var reader = ExcelReaderFactory.CreateOpenXmlReader(excelStream))
                {
                    jwr.WriteStartArray();
                    reader.Read();
                    var names = Range(0, reader.FieldCount)
                        .Select(i => reader[i].ToString())
                        .ToArray();
                    var objectCount = 0;
                    while (reader.Read())
                    {
                        jwr.WriteStartObject();
                        foreach (var i in Range(0, reader.FieldCount))
                        {
                            jwr.WritePropertyName(names[i]);
                            jwr.WriteValue(reader[i]);
                        }

                        jwr.WriteEndObject();
                        objectCount += 1;
                    }
                    singular = objectCount == 1;
                    jwr.WriteEndArray();
                }
                return jsonStream.ToArray();
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
    public class ExcelFormatError : BadRequest
    {
        internal ExcelFormatError(string message, Exception ie) : base(ErrorCodes.ExcelReaderError,
            $"RESTar was unable to write entities to excel. {message}. ", ie) { }
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an error when reading from Excel
    /// </summary>
    public class ExcelInputError : BadRequest
    {
        internal ExcelInputError(string message) : base(ErrorCodes.ExcelReaderError,
            "There was a format error in the excel input. Check that the file is being transmitted properly. In " +
            "curl, make sure the flag '--data-binary' is used and not '--data' or '-d'. Message: " + message) { }
    }
}