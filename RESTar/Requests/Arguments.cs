﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using RESTar.Internal;
using RESTar.Linq;
using RESTar.Logging;
using RESTar.OData;
using RESTar.Results.Error;
using IResource = RESTar.Internal.IResource;

namespace RESTar.Requests
{
    /// <summary>
    /// Contains parameters for a RESTar URI
    /// </summary>
    public interface IUriParameters
    {
        /// <summary>
        /// Specifies the resource for the request
        /// </summary>
        string ResourceSpecifier { get; }

        /// <summary>
        /// Specifies the view for the request
        /// </summary>
        string ViewName { get; }

        /// <summary>
        /// Specifies the conditions for the request
        /// </summary>
        List<UriCondition> Conditions { get; }

        /// <summary>
        /// Specifies the meta-conditions for the request
        /// </summary>
        List<UriCondition> MetaConditions { get; }
    }

    /// <summary>
    /// A RESTar class that defines the arguments that are used when creating a RESTar request to evaluate 
    /// an incoming call. Arguments is a unified way to talk about the input to request evaluation, 
    /// regardless of protocol and web technologies.
    /// </summary>
    internal class Arguments : ILogable, ITraceable
    {
        public Action Action { get; }
        private URI uri;
        private string UnparsedUri { get; }

        public URI Uri
        {
            get => uri;
            private set
            {
                BodyBytes = BodyBytes ?? value?.Macro?.BodyBinary.ToArray();
                value?.Macro?.HeadersDictionary?.ForEach(pair =>
                {
                    var currentValue = Headers.SafeGet(pair.Key);
                    if (string.IsNullOrWhiteSpace(currentValue) || currentValue == "*/*")
                        Headers[pair.Key] = pair.Value;
                });
                uri = value;
            }
        }

        private IResource iresource;
        public IResource IResource => iresource ?? (iresource = Resource.Find(Uri.ResourceSpecifier));
        public TCPConnection TcpConnection { get; }
        public byte[] BodyBytes { get; private set; }
        public Headers Headers { get; }
        public MimeType ContentType { get; }
        public MimeType Accept { get; }
        public ResultFinalizer ResultFinalizer { get; }
        internal string AuthToken { get; set; }
        internal Exception Error { get; set; }
        public string TraceId { get; }
        public bool ExcludeHeaders => IResource is IEntityResource e && e.RequiresAuthentication;

        LogEventType ILogable.LogEventType { get; } = LogEventType.HttpInput;
        string ILogable.LogMessage => $"{Action} {UnparsedUri}{(BodyBytes?.Length > 0 ? $" ({BodyBytes.Length} bytes)" : "")}";
        private string _contentString;

        string ILogable.LogContent
        {
            get
            {
                if (BodyBytes == null) return null;
                return _contentString ?? (_contentString = Encoding.UTF8.GetString(BodyBytes));
            }
        }

        public string HeadersStringCache { get; set; }

        public void ThrowIfError()
        {
            if (Error != null) throw Error;
        }

        private static bool PercentCharsEscaped(IDictionary<string, string> headers)
        {
            return headers?.ContainsKey("X-ARR-LOG-ID") == true;
        }

        private static string UnpackUriKey(string uriKey)
        {
            return uriKey != null ? HttpUtility.UrlDecode(uriKey).Substring(1, uriKey.Length - 2) : null;
        }

        internal Arguments(Action action, ref string query, byte[] body, Headers headers, TCPConnection tcpConnection)
        {
            TraceId = tcpConnection.TraceId;
            Action = action;
            Headers = headers ?? new Headers();
            Uri = URI.ParseInternal(ref query, PercentCharsEscaped(headers), out var key);
            if (key != null)
                Headers["Authorization"] = $"apikey {UnpackUriKey(key)}";
            if (tcpConnection.HasWebSocket && uri.ResourceSpecifier == URI.DefaultResourceSpecifier)
                Uri.ResourceSpecifier = "RESTar.Shell";
            UnparsedUri = query;
            BodyBytes = body;
            TcpConnection = tcpConnection;
            ContentType = MimeType.Parse(Headers["Content-Type"]);
            Accept = MimeType.ParseMany(Headers["Accept"]);
            switch (uri.Protocol)
            {
                case RESTProtocols.RESTar:
                    ResultFinalizer = RESTarProtocolProvider.FinalizeResult;
                    break;
                case RESTProtocols.OData:
                    if (!ODataProtocolProvider.IsCompliant(this, out var odataError))
                    {
                        Error = odataError;
                        return;
                    }
                    ResultFinalizer = ODataProtocolProvider.FinalizeResult;
                    break;
            }
            if (ContentType.TypeCode == MimeTypeCode.Unsupported)
                Error = new UnsupportedContent(ContentType);
            if (Accept.TypeCode == MimeTypeCode.Unsupported)
                Error = new NotAcceptable(Accept);
            if (uri.HasError)
                Error = uri.Error;
        }
    }
}