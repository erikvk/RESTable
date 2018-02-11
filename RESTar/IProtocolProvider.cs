using RESTar.Operations;
using RESTar.Requests;

namespace RESTar
{
    /// <summary>
    /// Interface for RESTar protocol providers. Protocol providers provide the logic for 
    /// parsing requests according to some protocol.
    /// </summary>
    public interface IProtocolProvider
    {
        /// <summary>
        /// The identifier is used in requests to indicate the protocol to use
        /// </summary>
        string ProtocolIdentifier { get; }

        /// <summary>
        /// Reads the query string, parses its content according to some protocol and populates 
        /// the URI
        /// </summary>
        void ParseQuery(string query, URI uri);

        /// <summary>
        /// If headers are used to check protocol versions, for example, this method allows the 
        /// protocolprovider to throw an exception and abort a request if the request is not 
        /// in compliance with the protocol.
        /// </summary>
        /// <param name="headers"></param>
        void CheckCompliance(Headers headers);

        /// <summary>
        /// Generates a relative URI string from an IUriParameters instance
        /// </summary>
        string MakeRelativeUri(IUriParameters parameters);

        /// <summary>
        /// Takes a result and generates an IFinalizedResult entity from it, that can be returned 
        /// to the network component.
        /// </summary>
        IFinalizedResult FinalizeResult(Result result);
    }
}