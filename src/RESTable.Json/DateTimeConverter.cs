using System;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace RESTable.Json
{
    internal class DateTimeConverter : IsoDateTimeConverter
    {
        internal DateTimeConverter(string formatString)
        {
            DateTimeFormat = formatString;
            DateTimeStyles = DateTimeStyles.AssumeUniversal;
        }

        /// <inheritdoc />
        /// <summary>
        /// This really should not be necessary, but DateTimeStyles does not work when writing JSON. It
        /// keeps assuming local when kind is unspecified. Hence this workaround.
        /// </summary>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is DateTime dt)
                base.WriteJson(writer, DateTime.SpecifyKind(dt, DateTimeKind.Utc).ToUniversalTime(), serializer);
            else base.WriteJson(writer, value, serializer);
        }
    }
}