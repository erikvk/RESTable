using RESTar.Internal;
using RESTar.Requests;

namespace RESTar
{
    /// <summary>
    /// A common interface for RESTar requests
    /// </summary>
    public interface IRequest
    {
        /// <summary>
        /// The method of the request
        /// </summary>
        RESTarMethods Method { get; }

        /// <summary>
        /// The resource of the request
        /// </summary>
        IResource Resource { get; }

        /// <summary>
        /// The conditions of the request
        /// </summary>
        Conditions Conditions { get; }

        /// <summary>
        /// The meta-conditions of the request
        /// </summary>
        MetaConditions MetaConditions { get; }

        /// <summary>
        /// The body of the request
        /// </summary>
        string Body { get; }

        /// <summary>
        /// The auth token assigned to this request
        /// </summary>
        string AuthToken { get; }
    }
}