using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json.Converters;

namespace RESTar.ContentTypeProviders
{
    internal class DateTimeConverter : IsoDateTimeConverter
    {
        internal static IDictionary<string, DateTimeConverter> Converters { get; }
        static DateTimeConverter() => Converters = new ConcurrentDictionary<string, DateTimeConverter>();

        internal DateTimeConverter(string formatString)
        {
            DateTimeStyles = DateTimeStyles.AssumeUniversal;
            DateTimeFormat = formatString;
        }
    }
}
