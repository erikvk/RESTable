using System;
using System.Diagnostics;
using System.Globalization;
using RESTable.Internal;
using RESTable.Requests;

namespace RESTable.Results
{
    /// <inheritdoc cref="ISerializedResult" />
    /// <inheritdoc cref="IResult" />
    /// <summary>
    /// The successful result of a RESTable request operation
    /// </summary>
    public abstract class RequestSuccess : Success
    {
        /// <summary>
        /// The request that generated this result
        /// </summary>
        public IRequest Request { get; }

        private IRequestInternal RequestInternal { get; }

        public override Body Body { get; }

        /// <inheritdoc />
        public override ISerializedResult Serialize()
        {
            if (IsSerialized) return this;
            var stopwatch = Stopwatch.StartNew();
            ISerializedResult result = this;
            try
            {
                var protocolProvider = RequestInternal.CachedProtocolProvider.ProtocolProvider;
                var acceptProvider = RequestInternal.GetOutputContentTypeProvider();
                return protocolProvider.Serialize(this, acceptProvider);
            }
            catch (Exception exception)
            {
                result.Body?.Dispose();
                return exception.AsResultOf(RequestInternal).Serialize();
            }
            finally
            {
                IsSerialized = true;
                stopwatch.Stop();
                TimeElapsed += stopwatch.Elapsed;
                Headers.Elapsed = TimeElapsed.TotalMilliseconds.ToString(CultureInfo.InvariantCulture);
            }
        }

        internal RequestSuccess(IRequest request) : base(request)
        {
            Request = request;
            TimeElapsed = request.TimeElapsed;
            RequestInternal = (IRequestInternal) request;
            Body = Body.CreateOutputBody(request);
        }
    }
}