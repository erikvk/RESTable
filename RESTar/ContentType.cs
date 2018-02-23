using System;
using System.Collections.Generic;
using System.Linq;
using ClosedXML.Excel;

namespace RESTar
{
    /// <summary>
    /// Describes a content type
    /// </summary>
    public struct ContentType
    {
        /// <summary>
        /// The MIME type string, for example "application/json"
        /// </summary>
        public string MimeType { get; }

        /// <summary>
        /// The additional data contained in the header
        /// </summary>
        public IReadOnlyDictionary<string, string> Data { get; }

        /// <summary>
        /// The Q value for the MIME type
        /// </summary>
        public decimal Q { get; }

        private static readonly ContentType DefaultInput = new ContentType("application/json");
        private static readonly ContentType DefaultOutput = new ContentType("*/*");

        internal static ContentType ParseInput(string contentTypeHeaderValue)
        {
            if (string.IsNullOrEmpty(contentTypeHeaderValue)) return DefaultInput;
            return new ContentType(contentTypeHeaderValue);
        }

        internal static ContentType ParseOutput(string headerValue)
        {
            if (string.IsNullOrEmpty(headerValue)) return DefaultOutput;
            return new ContentType(headerValue);
        }

        internal static ContentType[] ParseManyOutput(string headerValue)
        {
            return headerValue?.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => new ContentType(s))
                .OrderByDescending(m => m.Q)
                .ToArray();
        }

        /// <summary>
        /// Creates a new ContentType from a header value or MIME type, for example "application/json" or "application/json;charset=utf-8"
        /// </summary>
        /// <param name="headerValue"></param>
        public ContentType(string headerValue)
        {
            if (string.IsNullOrWhiteSpace(headerValue))
                headerValue = "*/*";
            var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Q = 1;
            var parts = headerValue.ToLower().Split(';');
            MimeType = parts[0].Trim();
            parts.Skip(1).Select(i => i.TSplit('=')).ForEach(data.TPut);
            if (data.TryGetValue("q", out var qs) && decimal.TryParse(qs, out var q))
                Q = q;
            Data = data;
        }

        /// <summary>
        /// Creates a header value from the MIME type
        /// </summary>
        public override string ToString()
        {
            var dataString = Data != null ? string.Join(";", Data.Select(d => $"{d.Key}={d.Value}")) : null;
            return $"{MimeType}{(dataString?.Length > 0 ? ";" + dataString : "")}";
        }

        /// <summary>
        /// Converts a header value string to a ContenType
        /// </summary>
        public static implicit operator ContentType(string headerValue) => new ContentType(headerValue);
    }
}