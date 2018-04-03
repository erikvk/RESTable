using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using RESTar.Internal;
using RESTar.Requests;

namespace RESTar.Results
{
    /// <inheritdoc cref="ISerializedResult" />
    /// <inheritdoc cref="IResult" />
    /// <summary>
    /// The result of a RESTar request operation
    /// </summary>
    public abstract class RequestSuccess : Success
    {
        /// <summary>
        /// The request that generated this result
        /// </summary>
        public IRequest Request { get; }

        private IRequestInternal RequestInternal { get; }

        private Stream _body;
        private bool IsSerializing;

        /// <inheritdoc />
        public override Stream Body => _body ?? (IsSerializing ? _body = new RESTarOutputStreamController() : null);

        /// <inheritdoc />
        public override ISerializedResult Serialize(ContentType? contentType = null)
        {
            IsSerializing = true;
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var protocolProvider = RequestInternal.CachedProtocolProvider;
                var acceptProvider = ContentTypeController.ResolveOutputContentTypeProvider(RequestInternal, contentType);
                var serialized = protocolProvider.ProtocolProvider.Serialize(this, acceptProvider);
                if (serialized is RequestSuccess rr && rr._body is RESTarOutputStreamController rsc)
                    _body = rsc.Stream;
                if (_body?.CanRead == true)
                {
                    if (_body.Length == 0)
                        _body.Dispose();
                    else if (Headers.ContentType == null)
                        Headers.ContentType = acceptProvider.ContentType;
                }
                else _body = null;
                return serialized;
            }
            catch (Exception exception)
            {
                _body?.Dispose();
                return Error.GetResult(exception, RequestInternal).Serialize();
            }
            finally
            {
                IsSerializing = false;
                stopwatch.Stop();
                TimeElapsed = TimeElapsed + stopwatch.Elapsed;
                Headers["RESTar-elapsed-ms"] = TimeElapsed.TotalMilliseconds.ToString(CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// Generates a URI string from URI components, according to the protocol of this Content
        /// </summary>
        protected string GetUriString(IUriComponents components) => RequestInternal
            .CachedProtocolProvider
            .ProtocolProvider
            .MakeRelativeUri(components);

        internal RequestSuccess(IRequest request) : base(request)
        {
            Headers = new Headers();
            Request = request;
            TimeElapsed = request.TimeElapsed;
            RequestInternal = (IRequestInternal) request;
        }
    }
}