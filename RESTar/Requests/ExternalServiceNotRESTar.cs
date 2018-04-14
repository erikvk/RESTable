using System;
using RESTar.Internal;

namespace RESTar.Requests
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar found a non-RESTar response for a remote RESTar request
    /// </summary>
    public sealed class ExternalServiceNotRESTar : Results.NotFound
    {
        internal ExternalServiceNotRESTar(Uri uri, Exception ie = null) : base(ErrorCodes.ExternalServiceNotRESTar,
            $"A remote request was made to '{uri}', but the response was not recognized as a compatible RESTar service response", ie) { }
    }
}