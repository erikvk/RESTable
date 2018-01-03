using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using Newtonsoft.Json;
using RESTar.Deflection.Dynamic;
using RESTar.Http;
using RESTar.Internal;
using RESTar.Operations;
using static RESTar.Internal.ErrorCodes;
using IResource = RESTar.Internal.IResource;

namespace RESTar
{
    /// <summary>
    /// A super class for all custom RESTar exceptions
    /// </summary>
    public abstract class RESTarException : Exception, IFinalizedResult
    {
        /// <summary>
        /// The error code for this error
        /// </summary>
        public ErrorCodes ErrorCode { get; }

        /// <summary>
        /// The status code to use in HTTP responses
        /// </summary>
        public HttpStatusCode StatusCode { get; protected set; }

        /// <summary>
        /// The status description to use in HTTP responses
        /// </summary>
        public string StatusDescription { get; protected set; }

        /// <summary>
        /// The headers to use in HTTP responses
        /// </summary>
        public Dictionary<string, string> Headers { get; } = new Dictionary<string, string>();

        /// <summary>
        /// Does this result contain content?
        /// </summary>
        public bool HasContent { get; } = false;

        Stream IFinalizedResult.Body { get; } = null;
        string IFinalizedResult.ContentType { get; } = null;

        internal RESTarException(ErrorCodes code, string message) : base(message)
        {
            ErrorCode = code;
            Headers["RESTar-info"] = Message;
        }

        internal RESTarException(ErrorCodes code, string message, Exception ie) : base(message, ie)
        {
            ErrorCode = code;
            Headers["RESTar-info"] = Message;
        }
    }

    #region Non-results

    /// <inheritdoc />
    /// <summary>
    /// Thrown when a RESTar add-on could not be connected properly
    /// </summary>
    public class RESTarAddOnException : RESTarException
    {
        internal RESTarAddOnException(string message) : base(AddOnError, message) { }
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when a RESTar resource provider was found invalid
    /// </summary>
    public class ExternalResourceProviderException : RESTarException
    {
        internal ExternalResourceProviderException(string message) : base(ResourceProviderError,
            "An error was found in an external ResourceProvider: " + message) { }
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when a RESTar resource wrapper was invalid
    /// </summary>
    public class ResourceWrapperException : RESTarException
    {
        internal ResourceWrapperException(string message) : base(ResourceWrapperError, message) { }
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when an error was detected in a virtual resource declaration.
    /// </summary>
    public class VirtualResourceDeclarationException : RESTarException
    {
        internal VirtualResourceDeclarationException(string message) : base(InvalidVirtualResourceDeclaration, message) { }
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when an error was detected in a virtual resource declaration.
    /// </summary>
    public class ResourceDeclarationException : RESTarException
    {
        internal ResourceDeclarationException(string message) : base(InvalidResourceDeclaration, message) { }
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when an error was detected in a resource view declaration.
    /// </summary>
    public class ResourceViewDeclarationException : RESTarException
    {
        internal ResourceViewDeclarationException(Type view, string message) : base(InvalidResourceViewDeclaration,
            $"Invalid resource view declaration for view '{view.Name}' in Resource '{view.DeclaringType?.FullName}'. {message}") { }
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when a call is made to RESTar before RESTarConfig.Init() has been called.
    /// </summary>
    public class NotInitializedException : RESTarException
    {
        internal NotInitializedException() : base(NotInitialized,
            "A RESTar request was created before RESTarConfig.Init() was called. Always " +
            "initialize the RESTar instance before making calls to it.") { }
    }

    /// <summary>
    /// Thrown when a RESTar needed a configuration file, but did not get a configuration file path in the call to RESTarConfig.Init
    /// </summary>
    public class MissingConfigurationFile : RESTarException
    {
        internal MissingConfigurationFile(string message) : base(ErrorCodes.MissingConfigurationFile, message) { }
    }

    #endregion

    #region Other

    /// <summary>
    /// Thrown when a request had a non-supported Accept header
    /// </summary>
    public class NotAcceptable : RESTarException
    {
        internal NotAcceptable(MimeType unsupported) : base(ErrorCodes.NotAcceptable,
            $"Unsupported accept format: '{unsupported.TypeCodeString}'")
        {
            StatusCode = HttpStatusCode.NotAcceptable;
            StatusDescription = "Not acceptable";
        }
    }

    /// <summary>
    /// Thrown when a request had a non-supported Content-Type header
    /// </summary>
    public class UnsupportedContent : RESTarException
    {
        internal UnsupportedContent(MimeType unsupported) : base(ErrorCodes.UnsupportedContent,
            $"Unsupported content type: '{unsupported.TypeCodeString}'")
        {
            StatusCode = HttpStatusCode.UnsupportedMediaType;
            StatusDescription = "Unsupported media type";
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when a RESTar encounters an infinite loop when evaluating a request
    /// </summary>
    public class InfiniteLoop : RESTarException
    {
        internal InfiniteLoop() : base(InfiniteLoopDetected,
            "RESTar encountered a potentially infinite loop of recursive internal calls.")
        {
            StatusCode = (HttpStatusCode) 508;
            StatusDescription = "Infinite loop detected";
        }

        internal InfiniteLoop(string message) : base(InfiniteLoopDetected, message)
        {
            StatusCode = (HttpStatusCode) 508;
            StatusDescription = "Infinite loop detected";
        }
    }


    /// <inheritdoc />
    /// <summary>
    /// Thrown when a client does something that is forbidden
    /// </summary>
    public class Forbidden : RESTarException
    {
        internal Forbidden(ErrorCodes code, string message) : base(code, message)
        {
            StatusCode = HttpStatusCode.Forbidden;
            StatusDescription = "Forbidden";
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when a client tries to make an external request to an internal resource
    /// </summary>
    public class ResourceIsInternal : Forbidden
    {
        internal ResourceIsInternal(IResource resource) : base(ErrorCodes.ResourceIsInternal,
            $"Cannot make an external request to internal resource '{resource.Name}'") { }
    }

    /// <summary>
    /// Thrown when a clients tries to perform a forbidden action in the view
    /// </summary>
    public class NotAllowedViewAction : Forbidden
    {
        internal NotAllowedViewAction(ErrorCodes code, string message) : base(code, message) { }
    }

    #endregion

    #region Not found

    /// <summary>
    /// Exceptions that should be treated as bad requests
    /// </summary>
    public abstract class NotFound : RESTarException
    {
        internal NotFound(ErrorCodes code, string message) : base(code, message)
        {
            StatusCode = HttpStatusCode.BadRequest;
            StatusDescription = "Bad request";
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when no HTML was found for a resource view.
    /// </summary>
    public class NoHtml : NotFound
    {
        internal NoHtml(IResource resource, string matcher) : base(NoMatchingHtml,
            $"No matching HTML file found for resource '{resource.Name}'. Add a HTML file " +
            $"'{matcher}' to the 'wwwroot/resources' directory.") { }
    }


    /// <summary>
    /// Thrown when RESTar encounters an unknown or not implemented feature
    /// </summary>
    public class FeatureNotImplemented : RESTarException
    {
        internal FeatureNotImplemented(string message) : base(NotImplemented, message)
        {
            StatusCode = HttpStatusCode.NotImplemented;
            StatusDescription = "Not implemented";
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar cannot locate a resource using a given search string
    /// </summary>
    public class UnknownResource : NotFound
    {
        internal UnknownResource(string searchString) : base(ErrorCodes.UnknownResource,
            $"RESTar could not locate any resource by '{searchString}'.") { }
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar cannot locate a macro using a given search string
    /// </summary>
    public class UnknownMacro : NotFound
    {
        internal UnknownMacro(string searchString) : base(ErrorCodes.UnknownMacro,
            $"RESTar could not locate any macro by '{searchString}'.") { }
    }


    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar cannot locate a resource view using a given search string
    /// </summary>
    public class UnknownView : NotFound
    {
        internal UnknownView(string searchString, ITarget resource) : base(UnknownResourceView,
            $"RESTar could not locate any resource view in '{resource.Name}' by '{searchString}'.") { }
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar cannot find a property/column in a given resource by a given property name.
    /// </summary>
    public class UnknownProperty : NotFound
    {
        internal UnknownProperty(MemberInfo type, string str) : base(ErrorCodes.UnknownProperty,
            $"Could not find any property in {(type.HasAttribute<RESTarViewAttribute>() ? $"view '{type.Name}' or type '{Resource.Get(type.DeclaringType)?.Name}'" : $"type '{type.Name}'")} by '{str}'.") { }
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar expected a unique match for a resource, but found more than one.
    /// </summary>
    public class AmbiguousResourceException : NotFound
    {
        internal AmbiguousResourceException(string searchString) : base(AmbiguousResource,
            $"RESTar could not uniquely identify a resource by '{searchString}'. Try qualifying the name further. ") { }
    }

    #endregion

    #region Bad request

    /// <summary>
    /// Exceptions that should be treated as bad requests
    /// </summary>
    public class BadRequest : RESTarException
    {
        internal BadRequest(ErrorCodes code, string message) : base(code, message)
        {
            StatusCode = HttpStatusCode.BadRequest;
            StatusDescription = "Bad request";
        }

        internal BadRequest(ErrorCodes code, string message, Exception ie) : base(code, message, ie)
        {
            StatusCode = HttpStatusCode.BadRequest;
            StatusDescription = "Bad request";
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when a syntax error was discovered when parsing a request
    /// </summary>
    public class InvalidSyntax : BadRequest
    {
        internal InvalidSyntax(ErrorCodes errorCode, string message) : base(errorCode,
            "Syntax error while parsing request: " + message) { }
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when a provided operator was invalid
    /// </summary>
    public class OperatorException : InvalidSyntax
    {
        internal OperatorException(string c) : base(InvalidConditionOperator,
            $"Invalid or missing operator or separator ('&') for condition '{c}'. Always URI encode all equals ('=' -> '%3D') " +
            "and exclamation marks ('!' -> '%21') in condition literals to avoid capture with reserved characters.") { }
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an error reading Excel data
    /// </summary>
    public class ExcelInputError : BadRequest
    {
        internal ExcelInputError(string message) : base(ExcelReaderError,
            "There was a format error in the excel input. Check that the file is being transmitted properly. In " +
            "curl, make sure the flag '--data-binary' is used and not '--data' or '-d'. Message: " + message) { }
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an error writing to the Excel format
    /// </summary>
    public class ExcelFormatError : BadRequest
    {
        internal ExcelFormatError(string message, Exception ie) : base(ExcelReaderError,
            $"RESTar was unable to write entities to excel. {message}. ", ie) { }
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when a provided operator was forbidden for the given property
    /// </summary>
    public class ForbiddenConditionOperator : BadRequest
    {
        internal ForbiddenConditionOperator(string c, ITarget target, Operator found, Term term, IEnumerable<Operator> allowed)
            : base(InvalidConditionOperator, $"Forbidden operator for condition '{c}'. '{found}' is not allowed when " +
                                             $"comparing against '{term.Key}' in type '{target.Name}'. Allowed operators" +
                                             $": {string.Join(", ", allowed.Select(a => $"'{a.Common}'"))}") { }
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an error getting entities from an external data source.
    /// </summary>
    public class InvalidExternalSource : BadRequest
    {
        internal InvalidExternalSource(HttpRequest request, string message) : base(InvalidSourceData,
            $"RESTar could not get entities from source at '{request.URI}'. {message}") { }
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an error sending entities to an external data destination.
    /// </summary>
    public class InvalidExternalDestination : BadRequest
    {
        internal InvalidExternalDestination(HttpRequest request, string message) : base(InvalidDestination,
            $"RESTar could not upload entities to destination at '{request.URI}': {message}") { }
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when an invalid number of data entities was provided for a certain method.
    /// </summary>
    public class InvalidInputCount : BadRequest
    {
        internal InvalidInputCount() : base(DataSourceFormat,
            "Invalid input count. Expected object/row, but found array/multiple rows. " +
            "Only POST accepts multiple objects/rows as input.") { }
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when an entity of a resource declared as IValidatable fails validation
    /// </summary>
    public class ValidatableException : BadRequest
    {
        internal ValidatableException(string message) : base(InvalidResourceEntity, message) { }
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when a resource cannot be identified when registering an alias.
    /// </summary>
    public class UnknownResourceForAliasException : BadRequest
    {
        internal UnknownResourceForAliasException(string searchString, IResource match) : base(ErrorCodes.UnknownResource,
            "Resource alias assignments must be provided with fully qualified resource names. No match " +
            $"for '{searchString}'. {(match != null ? $"Did you mean '{match.Name}'? " : "")}") { }
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when an alias cannot be registered for a resource because it is already in use
    /// </summary>
    public class AliasAlreadyInUseException : BadRequest
    {
        internal AliasAlreadyInUseException(Admin.ResourceAlias alias) : base(AliasAlreadyInUse,
            $"Invalid Alias: '{alias.Alias}' is already in use for resource '{alias.IResource.Name}'") { }
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when an alias cannot be registered for a resource because it is the same as a resource name
    /// </summary>
    public class AliasEqualToResourceNameException : BadRequest
    {
        internal AliasEqualToResourceNameException(string alias) : base(AliasEqualToResourceName,
            $"Invalid Alias: '{alias}' is a resource name") { }
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when a uniquely matched entity in a resource was expected for a request,
    /// but multiple was found. 
    /// </summary>
    public class AmbiguousMatchException : BadRequest
    {
        internal AmbiguousMatchException(ITarget resource) : base(AmbiguousMatch,
            $"Expected a uniquely matched entity in resource '{resource.Name}', but found multiple. " +
            "Manipulating multiple entities is either unsupported or unsafe. Specify additional " +
            "conditions or use the 'unsafe' meta-condition") { }
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when an invalid members was detected in a resource declaration.
    /// </summary>
    public class ResourceMemberException : BadRequest
    {
        internal ResourceMemberException(string message) : base(InvalidResourceMember, message) { }
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar has run out of dynamic tables for allocation to new 
    /// dynamic resources.
    /// </summary>
    public class NoAvalailableDynamicTableException : BadRequest
    {
        internal NoAvalailableDynamicTableException() : base(NoAvalailableDynamicTable,
            "RESTar have no more unallocated dynamic tables. Remove an existing table and try again.") { }
    }

    /// <summary>
    /// Throw when a request for an unsupported OData protocol version was encountered
    /// </summary>
    public class UnsupportedODataVersion : BadRequest
    {
        internal UnsupportedODataVersion() : base(UnsupportedODataProtocolVersion,
            "Unsupported OData protocol version. Supported protocol version: 4.0") { }
    }

    internal class HttpRequestException : Exception
    {
        public HttpRequestException(string message) : base(message) { }
    }

    #endregion

    #region Aborted operations

    /// <summary>
    /// A common class for instances when an operation was aborted
    /// </summary>
    public class AbortedOperation<T> : RESTarException where T : class
    {
        internal AbortedOperation(ErrorCodes code, Exception ie, IRequest<T> request, string message = null) : base(code,
            message ?? (ie is JsonSerializationException || ie is JsonReaderException ? "JSON serialization error, check JSON syntax. " : ""))
        {
            StatusCode = HttpStatusCode.BadRequest;
            StatusDescription = "Bad request";
            Headers["RESTar-info"] = $"Aborted {request.Method} on resource '{request.Resource}' " +
                                     $"due to an error: {this.TotalMessage()}";
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an error selecting entities from a given resource.
    /// </summary>
    public class AbortedSelectorException<T> : AbortedOperation<T> where T : class
    {
        internal AbortedSelectorException(Exception ie, IRequest<T> request, string message = null)
            : base(AbortedSelect, ie, request, message) { }
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an error inserting entities into a given resource.
    /// </summary>
    public class AbortedInserterException<T> : AbortedOperation<T> where T : class
    {
        internal AbortedInserterException(Exception ie, IRequest<T> request, string message = null)
            : base(AbortedInsert, ie, request, message) { }
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an error updating entities in a given resource.
    /// </summary>
    public class AbortedUpdaterException<T> : AbortedOperation<T> where T : class
    {
        internal AbortedUpdaterException(Exception ie, IRequest<T> request, string message = null)
            : base(AbortedUpdate, ie, request, message) { }
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an error deleting entities from a given resource.
    /// </summary>
    public class AbortedDeleterException<T> : AbortedOperation<T> where T : class
    {
        internal AbortedDeleterException(Exception ie, IRequest<T> request, string message = null)
            : base(AbortedDelete, ie, request, message) { }
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an error counting entities in a given resource.
    /// </summary>
    public class AbortedCounterException<T> : AbortedOperation<T> where T : class
    {
        internal AbortedCounterException(Exception ie, IRequest<T> request, string message = null)
            : base(AbortedCount, ie, request, message) { }
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an error profiling a given resource.
    /// </summary>
    public class AbortedProfilerException<T> : AbortedOperation<T> where T : class
    {
        internal AbortedProfilerException(Exception ie, IRequest<T> request, string message = null)
            : base(AbortedCount, ie, request, message) { }
    }

    #endregion
}