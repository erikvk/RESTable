using System;
using System.Collections.Generic;
using System.Linq;

namespace RESTar
{
    /// <inheritdoc />
    /// <summary>
    /// A collection of ContentType instances
    /// </summary>
    public class ContentTypes : List<ContentType>
    {
        /// <inheritdoc />
        public override string ToString() => string.Join(",", this);

        /// <inheritdoc />
        public ContentTypes() { }

        /// <inheritdoc />
        public ContentTypes(IEnumerable<ContentType> collection) : base(collection) { }
    }

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

        /// <summary>
        /// Is this content type defined as */* ?
        /// </summary>
        public bool AnyType => MimeType == "*/*";

        /// <summary>
        /// The default input content type (application/json)
        /// </summary>
        public static readonly ContentType DefaultInput = new ContentType("application/json");

        /// <summary>
        /// The default output content type (*/*)
        /// </summary>
        public static readonly ContentType DefaultOutput = new ContentType("*/*");

        /// <summary>
        /// Parses a Content-Type header an returnes a ContentType instance describing it
        /// </summary>
        public static ContentType ParseInput(string contentTypeHeaderValue)
        {
            if (string.IsNullOrWhiteSpace(contentTypeHeaderValue)) return DefaultInput;
            return new ContentType(contentTypeHeaderValue);
        }

        /// <summary>
        /// Parses an Accept header an returnes a ContentType instance describing it
        /// </summary>
        public static ContentType ParseOutput(string headerValue)
        {
            if (string.IsNullOrWhiteSpace(headerValue)) return DefaultOutput;
            return new ContentType(headerValue);
        }

        /// <summary>
        /// Parses an Accept header, possibly with multiple content types, an returnes an 
        /// array of ContentTypes describing it
        /// </summary>
        public static ContentTypes ParseManyOutput(string headerValue)
        {
            if (string.IsNullOrWhiteSpace(headerValue))
                return new ContentTypes {DefaultOutput};
            return new ContentTypes(headerValue
                .Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => new ContentType(s))
                .OrderByDescending(m => m.Q)
                .ToArray()
            );
        }

        /// <summary>
        /// Creates a new ContentType from a header value or MIME type, for example "application/json" or "application/json;charset=utf-8"
        /// </summary>
        /// <param name="headerValue"></param>
        public ContentType(string headerValue)
        {
            if (string.IsNullOrWhiteSpace(headerValue))
                headerValue = "*/*";
            if (headerValue.Contains(','))
            {
                var preferred = ParseManyOutput(headerValue)[0];
                MimeType = preferred.MimeType;
                Data = preferred.Data;
                Q = preferred.Q;
                return;
            }
            Q = 1;
            var parts = headerValue.ToLower().Split(';');
            MimeType = parts[0].Trim();
            var data = default(Dictionary<string, string>);
            foreach (var pair in parts.Skip(1).Select(i => i.TSplit('=')))
            {
                if (data == null)
                    data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                data.TPut(pair);
            }
            if (data == null)
            {
                Data = null;
                return;
            }
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