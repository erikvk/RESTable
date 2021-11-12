using System;
using System.Text.RegularExpressions;

namespace RESTable.AspNetCore
{
    public class RESTableAspNetCoreConfiguration
    {
        public const string ConfigSection = "RESTable.AspNetCore";
        private const string BaseUriRegex = @"^/?[\/\w]+$";
        private const string DefaultRootUri = "/restable";

        private string? rootUri;

        public string RootUri
        {
            get => rootUri ?? DefaultRootUri;
            set
            {
                ValidateRootUri(ref value);
                rootUri = value;
            }
        }

        private static void ValidateRootUri(ref string uri)
        {
            uri = uri.Trim();
            if (!Regex.IsMatch(uri, BaseUriRegex))
                throw new FormatException("The URI contained invalid characters. It can only contain " +
                                          "letters, numbers, forward slashes and underscores");
            if (uri[0] != '/') uri = $"/{uri}";
            uri = uri.TrimEnd('/');
        }
    }
}