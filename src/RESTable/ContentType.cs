﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;

namespace RESTable
{
    /// <summary>
    /// Describes a content type
    /// </summary>
    public struct ContentType
    {
        /// <summary>
        /// The media type string, for example "application/json" 
        /// </summary>
        public string MediaType { get; }

        /// <summary>
        /// The additional data contained in the header
        /// </summary>
        public IReadOnlyDictionary<string, string> Data { get; }

        /// <summary>
        /// The character set defined in the content type
        /// </summary>
        public string CharSet => Data.TryGetValue("charset", out var charset) ? charset : null;

        /// <summary>
        /// The Q value for the MIME type
        /// </summary>
        public decimal Q { get; }

        /// <summary>
        /// Is this content type defined as */* ?
        /// </summary>
        public bool AnyType => MediaType == "*/*";

        internal bool IsDefault => this == default;

        /// <summary>
        /// application/json
        /// </summary>
        public static readonly ContentType JSON = new("application/json");

        /// <summary>
        /// application/vnd.openxmlformats-officedocument.spreadsheetml.sheet
        /// </summary>
        public static readonly ContentType Excel = new("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

        /// <summary>
        /// application/xml
        /// </summary>
        public static readonly ContentType XML = new("application/xml");

        /// <summary>
        /// The default input content type (application/json)
        /// </summary>
        public static readonly ContentType DefaultInput = JSON;

        /// <summary>
        /// The default output content type (*/*)
        /// </summary>
        public static readonly ContentType DefaultOutput = new("*/*");

        /// <summary>
        /// Parses a header value an returnes a ContentType instance describing it
        /// </summary>
        internal static ContentType Parse(string headerValue)
        {
            if (string.IsNullOrWhiteSpace(headerValue))
                throw new ArgumentException("Cannot be null or whitespace", nameof(headerValue));
            return new ContentType(headerValue);
        }

        /// <summary>
        /// Parses a header value, possibly containing multiple content types, an returnes a 
        /// ContentTypes collection describing them.
        /// </summary>
        public static ContentTypes ParseMany(string headerValue)
        {
            if (string.IsNullOrWhiteSpace(headerValue))
                throw new ArgumentException("Cannot be null or whitespace", nameof(headerValue));
            return new ContentTypes(headerValue
                .Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => new ContentType(s))
                .OrderByDescending(m => m.Q)
                .ToArray()
            );
        }

        /// <summary>
        /// Creates a new ContentType from a header value or MIME type, for example "application/json" or "application/json; charset=utf-8"
        /// </summary>
        /// <param name="headerValue"></param>
        private ContentType(string headerValue)
        {
            if (string.IsNullOrWhiteSpace(headerValue))
                headerValue = "*/*";
            if (headerValue.Contains(','))
            {
                var preferred = ParseMany(headerValue)[0];
                MediaType = preferred.MediaType;
                Data = preferred.Data;
                Q = preferred.Q;
                return;
            }
            Q = 1;
            var parts = headerValue.ToLower().Split(';');
            var mimeTypePart = parts[0].Trim();
            MediaType = mimeTypePart;
            var data = default(Dictionary<string, string>);
            foreach (var pair in parts.Skip(1).Select(i => i.TSplit('=', true)))
            {
                data ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                data.TPut(pair);
            }
            if (data == null)
            {
                Data = null;
                return;
            }
            if (data.TryGetValue("q", out var qString) && decimal.TryParse(qString, out var q))
                Q = q;
            Data = data;
        }

        /// <summary>
        /// Creates a header value from the MIME type
        /// </summary>
        public override string ToString()
        {
            var dataString = Data != null ? string.Join(";", Data.Select(d => $"{d.Key}={d.Value}")) : null;
            return $"{MediaType}{(dataString?.Length > 0 ? ";" + dataString : "")}";
        }

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is ContentType ct && ct == this;

        /// <summary>
        /// Converts a header value string to a ContenType
        /// </summary>
        public static implicit operator ContentType(string headerValue) => new(headerValue);

        /// <summary>
        /// Converts a header value string to a ContenType
        /// </summary>
        public static implicit operator ContentType(System.Net.Mime.ContentType contentType) => new(contentType.ToString());

        /// <summary>
        /// Converts a header value string to a ContenType
        /// </summary>
        public static implicit operator System.Net.Mime.ContentType(ContentType contentType) => new(contentType.ToString());

        /// <summary>
        /// Converts a header value string to a ContenType
        /// </summary>
        public static implicit operator MediaTypeHeaderValue(ContentType contentType)
        {
            return MediaTypeHeaderValue.Parse(contentType.ToString());
        }

        /// <summary>
        /// Compares two content types for equality
        /// </summary>
        public static bool operator ==(ContentType first, ContentType second) => first.MediaType.EqualsNoCase(second.MediaType);

        /// <summary>
        /// Compares two content types for non-equality
        /// </summary>
        public static bool operator !=(ContentType first, ContentType second) => !first.MediaType.EqualsNoCase(second.MediaType);

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = MediaType != null ? MediaType.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ (Data != null ? Data.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Q.GetHashCode();
                return hashCode;
            }
        }
    }
}