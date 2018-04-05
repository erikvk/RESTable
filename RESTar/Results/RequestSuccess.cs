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
        public override Stream Body
        {
            get => _body ?? (IsSerializing ? _body = new RESTarOutputStreamController() : null);
            set => _body = value;
        }

        /// <inheritdoc />
        public override ISerializedResult Serialize(ContentType? contentType = null)
        {
            IsSerializing = true;
            var stopwatch = Stopwatch.StartNew();
            ISerializedResult result = this;
            try
            {
                var protocolProvider = RequestInternal.CachedProtocolProvider.ProtocolProvider;
                var acceptProvider = ContentTypeController.ResolveOutputContentTypeProvider(RequestInternal, contentType);
                result = protocolProvider.Serialize(this, acceptProvider);
                if (result.Body is RESTarOutputStreamController rsc)
                    result.Body = rsc.Stream;
                if (result.Body?.CanRead == true)
                {
                    if (result.Body.Length == 0)
                    {
                        result.Body.Dispose();
                        result.Body = null;
                    }
                    else if (Headers.ContentType == null)
                        Headers.ContentType = acceptProvider.ContentType;
                }
                else result.Body = null;
                return result;
            }
            catch (Exception exception)
            {
                result.Body?.Dispose();
                return Error.GetResult(exception, RequestInternal).Serialize();
            }
            finally
            {
                IsSerializing = false;
                IsSerialized = true;
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