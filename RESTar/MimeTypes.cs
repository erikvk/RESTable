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
        internal Dictionary<string, string> Data { get; } = new Dictionary<string, string>();
        internal decimal Q { get; } = 1;

        internal static readonly MimeType Default = new MimeType(Json);
        internal static readonly MimeType[] DefaultArray = {Default};

        internal static MimeType Parse(string headerValue)
        {
            if (string.IsNullOrEmpty(headerValue)) return Default;
            return new MimeType(headerValue);
        }

        internal static MimeType[] ParseMany(string headerValue)
        {
            if (string.IsNullOrEmpty(headerValue)) return DefaultArray;
            return headerValue.Split(',').Select(Parse).OrderByDescending(m => m.Q).ToArray();
        }

        private MimeType(MimeTypeCode code) => TypeCode = code;

        private MimeType(string headerValue)
        {
            var parts = headerValue.ToLower().Split(';');
            switch (parts[0].Trim())
            {
                case "*/*":
                case "json":
                case "application/json":
                    TypeCode = Json;
                    break;
                case "excel":
                case "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet":
                    TypeCode = Excel;
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

        internal static MimeTypeCode Match(string mimeTypeString)
        {
            switch (mimeTypeString?.ToLower())
            {
                case "":
                case null:
                case "json":
                case JSON:
                case "*/*": return Json;
                case var unsupported: throw new NotAcceptable(unsupported);
            }
        }

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
    }

    internal static class MimeTypeExtensions
    {
        internal static string ToMimeString(this MimeTypeCode mime) => MimeTypes.GetString(mime);
    }
}