using System;

#pragma warning disable 1591

namespace RESTar
{
    /// <summary>
    /// Contains the supported mime types used in RESTar
    /// </summary>
    public struct MimeTypes
    {
        public const string Excel = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        public const string JSON = "application/json";
        public const string XML = "application/xml";

        internal static RESTarMimeType Match(string mimeTypeString)
        {
            switch (mimeTypeString?.ToLower())
            {
                case XML: return RESTarMimeType.XML;
                case "excel":
                case Excel: return RESTarMimeType.Excel;
                default: return RESTarMimeType.Json;
            }
        }

        internal static string GetString(RESTarMimeType mimeType)
        {
            switch (mimeType)
            {
                case RESTarMimeType.Json: return JSON;
                case RESTarMimeType.Excel: return Excel;
                case RESTarMimeType.XML: return XML;
                default: throw new ArgumentOutOfRangeException(nameof(mimeType));
            }
        }
    }

    internal enum RESTarMimeType : byte
    {
        Json,
        Excel,
        XML
    }

    internal static class MimeTypeExtensions
    {
        internal static string ToMimeString(this RESTarMimeType mime) => MimeTypes.GetString(mime);
    }
}