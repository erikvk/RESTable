using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace RESTable.Requests
{
    public static class HeadersExtensions
    {
        public static IEnumerable<KeyValuePair<string, string>> GetCustom(this IHeadersInternal headers, HashSet<string> whitelist = null)
        {
            return headers.Where(pair => whitelist?.Contains(pair.Key) == true || IsCustomHeaderName(pair.Key));
        }

        internal static HashSet<string> NonCustomHeaders { get; }

        static HeadersExtensions() => NonCustomHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "host", "authorization", "connection", "upgrade", "restable-metadata", "sec-websocket-version", "sec-websocket-key",
            "sec-websocket-extensions"
        };

        public static bool IsCustomHeaderName(this string key) => !NonCustomHeaders.Contains(key);

        internal static string _Get(this IHeadersInternal headers, string key) => key switch
        {
            _ when key.EqualsNoCase(nameof(IHeaders.Accept)) => headers.Accept?.ToString(),
            _ when key.EqualsNoCase("Content-Type") => headers.ContentType?.ToString(),
            _ when key.EqualsNoCase(nameof(IHeaders.Source)) => headers.Source,
            _ when key.EqualsNoCase(nameof(IHeaders.Destination)) => headers.Destination,
            _ when key.EqualsNoCase(nameof(IHeaders.Authorization)) => headers.Authorization,
            _ when key.EqualsNoCase(nameof(IHeaders.Origin)) => headers.Origin,
            _ when key.EqualsNoCase("RESTable-elapsed-ms") => headers.Elapsed?.ToStringRESTable(),
            _ when headers.TryGetCustomHeader(key, out var value) => value,
            _ => default
        };

        internal static void _Set(this IHeadersInternal headers, string key, string value)
        {
            switch (key)
            {
                case var _ when key.EqualsNoCase(nameof(IHeaders.Accept)):
                    if (!string.IsNullOrWhiteSpace(value))
                        headers.Accept = ContentType.ParseMany(value);
                    break;
                case var _ when key.EqualsNoCase("Content-Type"):
                    if (!string.IsNullOrWhiteSpace(value))
                        headers.ContentType = ContentType.Parse(value);
                    break;
                case var _ when key.EqualsNoCase(nameof(IHeaders.Source)):
                    headers.Source = value;
                    break;
                case var _ when key.EqualsNoCase(nameof(IHeaders.Destination)):
                    headers.Destination = value;
                    break;
                case var _ when key.EqualsNoCase(nameof(IHeaders.Authorization)):
                    headers.Authorization = value;
                    break;
                case var _ when key.EqualsNoCase(nameof(IHeaders.Origin)):
                    headers.Origin = value;
                    break;
                case var _ when key.EqualsNoCase("RESTable-elapsed-ms"):
                    if (value == null)
                        headers.Elapsed = null;
                    else
                    {
                        var milliseconds = double.Parse(value, CultureInfo.InvariantCulture);
                        var timeSpan = TimeSpan.FromMilliseconds(milliseconds);
                        headers.Elapsed = timeSpan;
                    }
                    break;
                default:
                    headers.SetCustomHeader(key, value);
                    break;
            }
        }

        internal static bool _Contains(this IHeadersInternal headers, KeyValuePair<string, string> item) => item.Key switch
        {
            _ when item.Key.EqualsNoCase(nameof(IHeaders.Accept)) => headers.Accept?.ToString().EqualsNoCase(item.Value) ?? item.Value == null,
            _ when item.Key.EqualsNoCase("Content-Type") => headers.ContentType?.ToString().EqualsNoCase(item.Value) ?? item.Value == null,
            _ when item.Key.EqualsNoCase(nameof(IHeaders.Source)) => headers.Source.EqualsNoCase(item.Value),
            _ when item.Key.EqualsNoCase(nameof(IHeaders.Destination)) => headers.Destination.EqualsNoCase(item.Value),
            _ when item.Key.EqualsNoCase(nameof(IHeaders.Authorization)) => headers.Authorization.EqualsNoCase(item.Value),
            _ when item.Key.EqualsNoCase(nameof(IHeaders.Origin)) => headers.Origin.EqualsNoCase(item.Value),
            _ when item.Key.EqualsNoCase("RESTable-elapsed-ms") => Equals(headers.Elapsed?.ToStringRESTable(), item.Value),
            _ => headers.ContainsCustomHeader(item)
        };

        internal static bool _ContainsKey(this IHeadersInternal headers, string key) => key switch
        {
            _ when key.EqualsNoCase(nameof(IHeaders.Accept)) => headers.Accept != null,
            _ when key.EqualsNoCase("Content-Type") => headers.ContentType != null,
            _ when key.EqualsNoCase(nameof(IHeaders.Source)) => headers.Source != null,
            _ when key.EqualsNoCase(nameof(IHeaders.Destination)) => headers.Destination != null,
            _ when key.EqualsNoCase(nameof(IHeaders.Authorization)) => headers.Authorization != null,
            _ when key.EqualsNoCase(nameof(IHeaders.Origin)) => headers.Origin != null,
            _ when key.EqualsNoCase("RESTable-elapsed-ms") => headers.Elapsed != null,
            _ => headers.ContainsCustomHeaderName(key)
        };

        internal static bool _Remove(this IHeadersInternal headers, string key)
        {
            switch (key)
            {
                case var _ when key.EqualsNoCase(nameof(IHeaders.Accept)):
                    headers.Accept = null;
                    return true;
                case var _ when key.EqualsNoCase("Content-Type"):
                    headers.ContentType = null;
                    return true;
                case var _ when key.EqualsNoCase(nameof(IHeaders.Source)):
                    headers.Source = null;
                    return true;
                case var _ when key.EqualsNoCase(nameof(IHeaders.Destination)):
                    headers.Destination = null;
                    return true;
                case var _ when key.EqualsNoCase(nameof(IHeaders.Authorization)):
                    headers.Authorization = null;
                    return true;
                case var _ when key.EqualsNoCase(nameof(IHeaders.Origin)):
                    headers.Origin = null;
                    return true;
                case var _ when key.EqualsNoCase("RESTable-elapsed-ms"):
                    headers.Elapsed = null;
                    return true;
                default: return headers.RemoveCustomHeader(key);
            }
        }

        internal static bool _Remove(this IHeadersInternal headers, KeyValuePair<string, string> item)
        {
            switch (item.Key)
            {
                case var _ when item.Key.EqualsNoCase(nameof(IHeaders.Accept)) && item.Value == headers.Accept?.ToString():
                    headers.Accept = null;
                    return true;
                case var _ when item.Key.EqualsNoCase("Content-Type") && item.Value == headers.ContentType?.ToString():
                    headers.ContentType = null;
                    return true;
                case var _ when item.Key.EqualsNoCase(nameof(IHeaders.Source)) && item.Value == headers.Source:
                    headers.Source = null;
                    return true;
                case var _ when item.Key.EqualsNoCase(nameof(IHeaders.Destination)) && item.Value == headers.Destination:
                    headers.Destination = null;
                    return true;
                case var _ when item.Key.EqualsNoCase(nameof(IHeaders.Authorization)) && item.Value == headers.Authorization:
                    headers.Authorization = null;
                    return true;
                case var _ when item.Key.EqualsNoCase(nameof(IHeaders.Origin)) && item.Value == headers.Origin:
                    headers.Origin = null;
                    return true;
                case var _ when item.Key.EqualsNoCase("RESTable-elapsed-ms") && Equals(headers.Elapsed?.ToStringRESTable(), item.Value):
                    headers.Elapsed = null;
                    return true;
                default: return headers.RemoveCustomHeader(item);
            }
        }

        internal static bool _TryGetValue(this IHeadersInternal headers, string key, out string value)
        {
            switch (key)
            {
                case var _ when key.EqualsNoCase(nameof(IHeaders.Accept)):
                    value = headers.Accept?.ToString();
                    return value != null;
                case var _ when key.EqualsNoCase("Content-Type"):
                    value = headers.ContentType?.ToString();
                    return value != null;
                case var _ when key.EqualsNoCase(nameof(IHeaders.Source)):
                    value = headers.Source;
                    return value != null;
                case var _ when key.EqualsNoCase(nameof(IHeaders.Destination)):
                    value = headers.Destination;
                    return value != null;
                case var _ when key.EqualsNoCase(nameof(IHeaders.Authorization)):
                    value = headers.Authorization;
                    return value != null;
                case var _ when key.EqualsNoCase(nameof(IHeaders.Origin)):
                    value = headers.Origin;
                    return value != null;
                case var _ when key.EqualsNoCase("RESTable-elapsed-ms"):
                    value = headers.Elapsed?.ToStringRESTable();
                    return value != null;
                default: return headers.TryGetCustomHeader(key, out value);
            }
        }

        internal static void _CopyTo(this IHeadersInternal headers, KeyValuePair<string, string>[] array, int arrayIndex)
        {
            headers.ToList().CopyTo(array, arrayIndex);
        }

        internal static ICollection<string> _Keys(this IHeadersInternal headers) => headers.Select(kvp => kvp.Key).ToList();
        internal static ICollection<string> _Values(this IHeadersInternal headers) => headers.Select(kvp => kvp.Value).ToList();
    }
}