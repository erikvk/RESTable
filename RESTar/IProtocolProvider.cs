﻿using System.Collections.Generic;
using RESTar.Operations;
using RESTar.Queries;

namespace RESTar
{
    /// <summary>
    /// Enumeration used to configure how a RESTar protocol provider works with external 
    /// content type providers.
    /// </summary>
    public enum ExternalContentTypeProviderSettings
    {
        /// <summary>
        /// Allow all external content type providers
        /// </summary>
        AllowAll,

        /// <summary>
        /// Allow external content type providers only when deserializing request bodies
        /// </summary>
        AllowInput,

        /// <summary>
        /// Allow external content type providers only when serializing response bodies
        /// </summary>
        AllowOutput,

        /// <summary>
        /// Do not allow any external content type providers
        /// </summary>
        DontAllow
    }

    /// <summary>
    /// Interface for RESTar protocol providers. Protocol providers provide the logic for 
    /// parsing requests according to some protocol.
    /// </summary>
    public interface IProtocolProvider
    {
        /// <summary>
        /// The name of the protocol
        /// </summary>
        string ProtocolName { get; }

        /// <summary>
        /// The identifier is used in request URIs to indicate the protocol to use. If the ProtocolIdentifer 
        /// is 'OData', for example, and RESTar runs locally, on port 8282 and with root URI "/rest" requests 
        /// can trigger the OData protocol by "127.0.0.1:8282/rest-odata",
        /// </summary>
        string ProtocolIdentifier { get; }

        /// <summary>
        /// Configures how this protocol provider works with external content type providers, or if it should 
        /// only work with the ones specified in the GetContentTypeProviders method.
        /// </summary>
        ExternalContentTypeProviderSettings ExternalContentTypeProviderSettings { get; }

        /// <summary>
        /// Gets the content type providers associated with this protocol provider. If this is the exclusive list 
        /// of content type providers to use with this protocol, set the AllowExternalContentProviders property to false.
        /// If only external content type providers should be used, return null.
        /// </summary>
        /// <returns></returns>
        IEnumerable<IContentTypeProvider> GetContentTypeProviders();

        /// <summary>
        /// Reads a query string, which is everyting after the root URI in the full request URI, parses 
        /// its content according to some protocol and populates the URI object.
        /// </summary>
        void ParseQuery(string query, URI uri, Context context);

        /// <summary>
        /// If headers are used to check protocol versions, for example, this method allows the 
        /// protocolprovider to throw an exception and abort a request if the API call is not 
        /// in compliance with the protocol.
        /// </summary>
        bool IsCompliant(IQuery query, out string invalidReason);

        /// <summary>
        /// The protocol needs to be able to generate a relative URI string from an IUriParameters instance. 
        /// Note that only components added to a URI in ParseQuery can be present in the IUriParameters instance.
        /// </summary>
        string MakeRelativeUri(IUriComponents components);

        /// <summary>
        /// Takes a result and generates an ISerializedResult entity from it, that can be returned 
        /// to the network component.
        /// </summary>
        ISerializedResult Serialize(IResult result, IContentTypeProvider contentTypeProvider);
    }
}