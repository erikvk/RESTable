using System;
using System.Collections.Generic;
using System.Linq;
using ClosedXML.Excel;
using static RESTar.MimeTypeCode;

#pragma warning disable 1591

namespace RESTar
{
    public class MimeType
    {
        internal MimeTypeCode TypeCode { get; }
        internal string TypeCodeString { get; }
        internal Dictionary<string, string> Data { get; } = new Dictionary<string, string>();
        internal decimal Q { get; } = 1;

        internal static readonly MimeType Default = new MimeType(Json, MimeTypes.JSON);

        internal static MimeType Parse(string headerValue)
        {
            if (string.IsNullOrEmpty(headerValue)) return Default;
            return new MimeType(headerValue);
        }

        internal static MimeType ParseMany(string headerValue)
        {
            if (string.IsNullOrEmpty(headerValue)) return Default;
            var found = headerValue.Split(',')
                .Select(Parse)
                .OrderByDescending(m => m.Q)
                .FirstOrDefault(m => m.TypeCode != Unsupported);
            return found ?? new MimeType(Unsupported, headerValue);
        }

        private MimeType(MimeTypeCode code, string codeString)
        {
            TypeCode = code;
            TypeCodeString = codeString;
        }

        private MimeType(string headerValue)
        {
            var parts = headerValue.ToLower().Split(';');
            TypeCodeString = parts[0].Trim();
            switch (TypeCodeString)
            {
                case "*/*":
                case "json":
                case "application/json":
                case "application/x-www-form-urlencoded":
                case "application/octet-stream":
                    TypeCode = Json;
                    break;
                case "excel":
                case "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet":
                    TypeCode = Excel;
                    break;
                case "application/xml":
                    TypeCode = XML;
                    break;
                default:
                    TypeCode = Unsupported;
                    break;
            }

            parts.Skip(1).Select(i => i.TSplit('=')).ForEach(Data.TPut);
            if (Data.TryGetValue("q", out var qs) && decimal.TryParse(qs, out var q))
                Q = q;
        }

        public override string ToString()
        {
            var dataString = string.Join(";", Data.Select(d => $"{d.Key}={d.Value}"));
            return $"{MimeTypes.GetString(TypeCode)}{(dataString.Length > 0 ? ";" + dataString : "")}";
        }
    }

    /// <summary>
    /// Contains the supported mime types used in RESTar
    /// </summary>
    public static class MimeTypes
    {
        public const string Excel = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        public const string JSON = "application/json;charset=utf-8";
        public const string JSONOData = "application/json;odata.metadata=minimal;odata.streaming=true;charset=utf-8";

        internal static string GetString(MimeTypeCode mimeType)
        {
            switch (mimeType)
            {
                case Json: return JSON;
                case MimeTypeCode.Excel: return Excel;
                default: throw new ArgumentOutOfRangeException(nameof(mimeType));
            }
        }
    }

    public enum MimeTypeCode : byte
    {
        Unsupported,
        Json,
        Excel,
        XML
    }

    internal static class MimeTypeExtensions
    {
        internal static string ToMimeString(this MimeTypeCode mime) => MimeTypes.GetString(mime);
    }
}