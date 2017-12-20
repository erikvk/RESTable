using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using RESTar.Deflection.Dynamic;
using RESTar.Http;
using RESTar.Internal;
using RESTar.Operations;
using static RESTar.Internal.ErrorCodes;
using static RESTar.Requests.Results;
using static RESTar.Admin.Settings;
using IResource = RESTar.Internal.IResource;

namespace RESTar
{
    /// <inheritdoc />
    /// <summary>
    /// A super class for all custom RESTar exceptions
    /// </summary>
    public class RESTarException : Exception
    {
        /// <summary>
        /// The error code of the exception
        /// </summary>
        public readonly ErrorCodes ErrorCode;

        internal Result Result;
        internal RESTarException(ErrorCodes code, string message) : base(message) => ErrorCode = code;
        internal RESTarException(ErrorCodes code, string message, Exception ie) : base(message, ie) => ErrorCode = code;
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when a RESTar add-on could not be connected properly
    /// </summary>
    public class RESTarAddOnException : RESTarException
    {
        internal RESTarAddOnException(string message) : base(AddOnError, message) => Result = BadRequest(this);
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
    /// Thrown when a RESTar encounters an infinite loop when evaluating a request
    /// </summary>
    public class InfiniteLoopException : RESTarException
    {
        internal InfiniteLoopException() : base(InfiniteLoopDetected,
            "RESTar encountered a potentially infinite loop of recursive internal calls.") => Result = InfiniteLoop(this);

        internal InfiniteLoopException(string message) : base(InfiniteLoopDetected, message) => Result = InfiniteLoop(this);
    }


    /// <inheritdoc />
    /// <summary>
    /// Thrown when a client does something that is forbidden
    /// </summary>
    public class ForbiddenException : RESTarException
    {
        internal ForbiddenException(ErrorCodes code, string message) : base(code, message) => Result = Forbidden(message);
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when a client tries to make an external request to an internal resource
    /// </summary>
    public class ResourceIsInternalException : RESTarException
    {
        internal ResourceIsInternalException(IResource resource) : base(ResourceIsInternal,
            $"Cannot make an external request to internal resource '{resource.Name}'") =>
            Result = Forbidden("Resource unavailable");
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when no HTML was found for a resource view.
    /// </summary>
    public class NoHtmlException : RESTarException
    {
        internal NoHtmlException(IResource resource, string matcher) : base(NoMatchingHtml,
            $"No matching HTML file found for resource '{resource.Name}'. Add a HTML file " +
            $"'{matcher}' to the 'wwwroot/resources' directory.") => Result = NotFound(this);
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when a syntax error was discovered when parsing a request
    /// </summary>
    public class SyntaxException : RESTarException
    {
        internal SyntaxException(ErrorCodes errorCode, string message) : base(errorCode,
            "Syntax error while parsing request: " + message) => Result = BadRequest(this);
    }

    /// <summary>
    /// Thrown when RESTar encounters an unknown or not implemented feature
    /// </summary>
    public class FeatureNotImplementedException : RESTarException
    {
        internal FeatureNotImplementedException(string message) : base(NotImplemented, message) =>
            Result = FeatureNotImplemented(this);
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when a syntax error was discovered when parsing a request
    /// </summary>
    public class InvalidSeparatorException : RESTarException
    {
        internal InvalidSeparatorException() : base(InvalidSeparator,
            "Syntax error while parsing request: Invalid argument separator count. A RESTar URI can contain at most 3 " +
            $"forward slashes after the base uri. URI scheme: {_ResourcesPath}/[resource][-view]/[conditions]/[meta-conditions]") =>
            Result = BadRequest(this);
    }


    /// <inheritdoc />
    /// <summary>
    /// Thrown when a provided operator was invalid
    /// </summary>
    public class OperatorException : SyntaxException
    {
        internal OperatorException(string c) : base(InvalidConditionOperator,
            $"Invalid or missing operator or separator ('&') for condition '{c}'. Always URI encode all equals ('=' -> '%3D') " +
            "and exclamation marks ('!' -> '%21') in condition literals to avoid capture with reserved characters.") { }
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when a provided operator was forbidden for the given property
    /// </summary>
    public class ForbiddenOperatorException : RESTarException
    {
        internal ForbiddenOperatorException(string c, ITarget target, Operator found, Term term,
            IEnumerable<Operator> allowed) : base(InvalidConditionOperator,
            $"Invalid operator for condition '{c}'. Operator '{found}' is not allowed when " +
            $"comparing against '{term.Key}' in type '{target.Name}'. Allowed operators" +
            $": {string.Join(", ", allowed.Select(a => $"'{a.Common}'"))}") => Result = BadRequest(this);
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an error getting entities from an
    /// external data source.
    /// </summary>
    public class SourceException : RESTarException
    {
        internal SourceException(HttpRequest request, string message) : base(InvalidSourceData,
            $"RESTar could not get entities from source at '{request.URI}'. {message}") => Result = BadRequest(this);
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an error getting entities from an
    /// external data source.
    /// </summary>
    public class DestinationException : RESTarException
    {
        internal DestinationException(HttpRequest request, string message) : base(InvalidDestination,
            $"RESTar could not upload entities to destination at '{request.URI}': {message}") => Result =
            BadRequest(this);
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when an invalid number of data entities was provided for a certain
    /// method.
    /// </summary>
    public class InvalidInputCountException : RESTarException
    {
        internal InvalidInputCountException() : base(DataSourceFormat,
            "Invalid input count. Expected object/row, but found array/multiple rows. " +
            "Only POST accepts multiple objects/rows as input.") => Result = BadRequest(this);
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar cannot locate a resource using a given search string
    /// </summary>
    public class UnknownResourceException : RESTarException
    {
        internal UnknownResourceException(string searchString) : base(UnknownResource,
            $"RESTar could not locate any resource by '{searchString}'.") => Result = NotFound(this);
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar cannot locate a macro using a given search string
    /// </summary>
    public class UnknownMacroException : RESTarException
    {
        internal UnknownMacroException(string searchString) : base(UnknownMacro,
            $"RESTar could not locate any macro by '{searchString}'.") => Result = NotFound(this);
    }


    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar cannot locate a resource view using a given search string
    /// </summary>
    public class UnknownViewException : RESTarException
    {
        internal UnknownViewException(string searchString, ITarget resource) : base(UnknownResourceView,
            $"RESTar could not locate any resource view in '{resource.Name}' by '{searchString}'.") => Result = NotFound(this);
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an error reading Excel data
    /// </summary>
    public class ExcelInputException : RESTarException
    {
        internal ExcelInputException(string message) : base(ExcelReaderError,
            "There was a format error in the excel input. Check that the file is being transmitted properly. In " +
            "curl, make sure the flag '--data-binary' is used and not '--data' or '-d'. Message: " + message) =>
            Result = BadRequest(this);
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an error writing to the Excel format
    /// </summary>
    public class ExcelFormatException : RESTarException
    {
        internal ExcelFormatException(string message, Exception ie) : base(ExcelReaderError,
            $"RESTar was unable to write entities to excel. {message}. ", ie) => Result = BadRequest(this);
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar cannot find a property/column in a given resource by a given property name.
    /// </summary>
    public class UnknownPropertyException : RESTarException
    {
        internal UnknownPropertyException(Type type, string str) : base(UnknownProperty,
            $"Could not find any property in {(type.HasAttribute<RESTarViewAttribute>() ? $"view '{type.Name}' or resource '{Resource.Get(type.DeclaringType)?.Name}'" : $"resource '{type.Name}'")} by '{str}'.") =>
            Result = NotFound(this);
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar expected a unique match for a property/column in a resource, but found more than one.
    /// </summary>
    public class AmbiguousPropertyException : RESTarException
    {
        /// <summary>
        /// The possible candidates found
        /// </summary>
        public readonly IList<string> Candidates;

        /// <summary>
        /// The search string that was used
        /// </summary>
        public readonly string SearchString;

        internal AmbiguousPropertyException(Type type, string str, IEnumerable<string> cands)
            : base(ErrorCodes.AmbiguousProperty,
                $"Could not uniquely identify a property in type '{type.Name}' by " +
                $"'{str}'. Candidates: {string.Join(", ", cands)}. ")
        {
            SearchString = str;
            Candidates = cands.ToList();
            Result = AmbiguousProperty(this);
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar expected a unique match for a resource, but found more than one.
    /// </summary>
    public class AmbiguousResourceException : RESTarException
    {
        /// <summary>
        /// The search string that was used
        /// </summary>
        public readonly string SearchString;

        internal AmbiguousResourceException(string searchString) : base(ErrorCodes.AmbiguousResource,
            $"RESTar could not uniquely identify a resource by '{searchString}'. Try qualifying the name further. ")
        {
            SearchString = searchString;
            Result = AmbiguousResource(this);
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an error selecting entities from a given resource.
    /// </summary>
    public class AbortedSelectorException<T> : RESTarException where T : class
    {
        internal AbortedSelectorException(Exception ie, IRequest<T> request, string message = null)
            : base(AbortedSelect, message ?? (ie.GetType() == typeof(JsonSerializationException) ||
                                              ie.GetType() == typeof(JsonReaderException)
                                      ? "JSON serialization error, check JSON syntax. "
                                      : ""
                                  ), ie) => Result = AbortedOperation(this, request);
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an error inserting entities into a given resource.
    /// </summary>
    public class AbortedInserterException<T> : RESTarException where T : class
    {
        internal AbortedInserterException(Exception ie, IRequest<T> request, string message = null)
            : base(AbortedInsert, message ?? (ie.GetType() == typeof(JsonSerializationException) ||
                                              ie.GetType() == typeof(JsonReaderException)
                                      ? "JSON serialization error, check JSON syntax. "
                                      : ""
                                  ), ie) => Result = AbortedOperation(this, request);
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an error updating entities in a given resource.
    /// </summary>
    public class AbortedUpdaterException<T> : RESTarException where T : class
    {
        internal AbortedUpdaterException(Exception ie, IRequest<T> request, string message = null)
            : base(AbortedUpdate, message ?? (ie.GetType() == typeof(JsonSerializationException) ||
                                              ie.GetType() == typeof(JsonReaderException)
                                      ? "JSON serialization error, check JSON syntax. "
                                      : ""
                                  ), ie) => Result = AbortedOperation(this, request);
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an error deleting entities from a given resource.
    /// </summary>
    public class AbortedDeleterException<T> : RESTarException where T : class
    {
        internal AbortedDeleterException(Exception ie, IRequest<T> request, string message = null)
            : base(AbortedDelete, message ?? (ie.GetType() == typeof(JsonSerializationException) ||
                                              ie.GetType() == typeof(JsonReaderException)
                                      ? "JSON serialization error, check JSON syntax. "
                                      : ""
                                  ), ie) => Result = AbortedOperation(this, request);
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an error counting entities in a given resource.
    /// </summary>
    public class AbortedCounterException<T> : RESTarException where T : class
    {
        internal AbortedCounterException(Exception ie, IRequest<T> request, string message = null)
            : base(AbortedCount, message ?? (ie.GetType() == typeof(JsonSerializationException) ||
                                             ie.GetType() == typeof(JsonReaderException)
                                     ? "JSON serialization error, check JSON syntax. "
                                     : ""
                                 ), ie) => Result = AbortedOperation<T>(this, request);
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an error profiling a given resource.
    /// </summary>
    public class AbortedProfilerException<T> : RESTarException where T : class
    {
        internal AbortedProfilerException(Exception ie, IRequest<T> request, string message = null)
            : base(AbortedCount, message ?? (ie.GetType() == typeof(JsonSerializationException) ||
                                             ie.GetType() == typeof(JsonReaderException)
                                     ? "JSON serialization error, check JSON syntax. "
                                     : ""
                                 ), ie) => Result = AbortedOperation(this, request);
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when an entity of a resource declared as IValidatable fails validation
    /// </summary>
    public class ValidatableException : RESTarException
    {
        internal ValidatableException(string message) : base(InvalidResourceEntity, message)
            => Result = BadRequest(this);
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when a resource cannot be identified when registering an alias.
    /// </summary>
    public class UnknownResourceForAliasException : RESTarException
    {
        internal UnknownResourceForAliasException(string searchString, IResource match) : base(UnknownResource,
            "Resource alias assignments must be provided with fully qualified resource names. No match " +
            $"for '{searchString}'. {(match != null ? $"Did you mean '{match.Name}'? " : "")}")
            => Result = BadRequest(this);
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when an alias cannot be registered for a resource because it is already in use
    /// </summary>
    public class AliasAlreadyInUseException : RESTarException
    {
        internal AliasAlreadyInUseException(Admin.ResourceAlias alias) : base(AliasAlreadyInUse,
            $"Invalid Alias: '{alias.Alias}' is already in use for resource '{alias.IResource.Name}'") =>
            Result = BadRequest(this);
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when an alias cannot be registered for a resource because it is the same as a resource name
    /// </summary>
    public class AliasEqualToResourceNameException : RESTarException
    {
        internal AliasEqualToResourceNameException(string alias) : base(AliasEqualToResourceName,
            $"Invalid Alias: '{alias}' is a resource name") => Result = BadRequest(this);
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when a uniquely matched entity in a resource was expected for a request,
    /// but multiple was found. 
    /// </summary>
    public class AmbiguousMatchException : RESTarException
    {
        internal AmbiguousMatchException(ITarget resource) : base(AmbiguousMatch,
            $"Expected a uniquely matched entity in resource '{resource.Name}', but found multiple. " +
            "Manipulating multiple entities is either unsupported or unsafe. Specify additional " +
            "conditions or use the 'unsafe' meta-condition") => Result = BadRequest(this);
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when an invalid members was detected in a resource declaration.
    /// </summary>
    public class ResourceMemberException : RESTarException
    {
        internal ResourceMemberException(string message) : base(InvalidResourceMember, message) => Result = BadRequest(this);
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when an error was detected in a virtual resource declaration.
    /// </summary>
    public class VirtualResourceDeclarationException : RESTarException
    {
        internal VirtualResourceDeclarationException(string message) : base(InvalidVirtualResourceDeclaration, message) =>
            Result = BadRequest(this);
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when an error was detected in a virtual resource declaration.
    /// </summary>
    public class ResourceDeclarationException : RESTarException
    {
        internal ResourceDeclarationException(string message) : base(InvalidResourceDeclaration, message) =>
            Result = BadRequest(this);
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when an error was detected in a resource view declaration.
    /// </summary>
    public class ResourceViewDeclarationException : RESTarException
    {
        internal ResourceViewDeclarationException(Type view, string message) : base(InvalidResourceViewDeclaration,
            $"Invalid resource view declaration for view '{view.Name}' in Resource '{view.DeclaringType?.FullName}'. {message}") =>
            Result = BadRequest(this);
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar has run out of dynamic tables for allocation to new 
    /// dynamic resources.
    /// </summary>
    public class NoAvalailableDynamicTableException : RESTarException
    {
        internal NoAvalailableDynamicTableException() : base(NoAvalailableDynamicTable,
            "RESTar have no more unallocated dynamic tables. Remove an existing table and try again.") =>
            Result = BadRequest(this);
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when a call is made to RESTar before RESTarConfig.Init() has been called.
    /// </summary>
    public class NotInitializedException : RESTarException
    {
        internal NotInitializedException() : base(NotInitialized,
            "A RESTar request was created before RESTarConfig.Init() was called. Always " +
            "initialize the RESTar instance before making calls to it.") => Result = BadRequest(this);
    }

    internal class HttpRequestException : Exception
    {
        public HttpRequestException(string message) : base(message) { }
    }
}