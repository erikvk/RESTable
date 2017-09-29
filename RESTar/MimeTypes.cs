using System;

#pragma warning disable 1591

namespace RESTar
{
    /// <summary>
    /// Contains the supported mime types used in RESTar
    /// </summary>
    public static class MimeTypes
    {
        public const string Excel = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        public const string JSON = "application/json";
        public const string XML = "application/xml";

        internal static MimeType Match(string mimeTypeString)
        {
            switch (mimeTypeString?.ToLower())
            {
                case XML: return MimeType.XML;
                case "excel":
                case Excel: return MimeType.Excel;
                default: return MimeType.Json;
            }
        }

        internal static string GetString(MimeType mimeType)
        {
            switch (mimeType)
            {
                case MimeType.Json: return JSON;
                case MimeType.Excel: return Excel;
                case MimeType.XML: return XML;
                default: throw new ArgumentOutOfRangeException(nameof(mimeType));
            }
        }
    }

    internal enum MimeType : byte
    {
        Json,
        Excel,
        XML
    }

    internal static class MimeTypeExtensions
    {
        internal static string ToMimeString(this MimeType mime) => MimeTypes.GetString(mime);
    }
}