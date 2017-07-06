using System;
using System.Collections.Generic;
using System.Linq;
using RESTar.Deflection;
using RESTar.Internal;
using RESTar.View;
using Starcounter;
using static RESTar.Internal.ErrorCodes;
using static RESTar.Requests.Responses;
using static RESTar.Settings;
using static RESTar.View.MessageType;
using IResource = RESTar.Internal.IResource;

namespace RESTar
{
    /// <summary>
    /// A super class for all custom RESTar exceptions
    /// </summary>
    public class RESTarException : Exception
    {
        /// <summary>
        /// The error code of the exception
        /// </summary>
        public readonly ErrorCodes ErrorCode;

        internal Response Response;

        internal RESTarException(ErrorCodes code, string message) : base(message) => ErrorCode = code;
        internal RESTarException(ErrorCodes code, string message, Exception ie) : base(message, ie) => ErrorCode = code;

        internal static Json HandleViewException(Exception e)
        {
            var re = e as RESTarException;
            var master = Self.GET<View.Page>("/__restar/__page");
            var partial = master.CurrentPage as IRESTarView ?? new MessageWindow().Populate();
            partial.SetMessage(e.Message, re?.ErrorCode ?? UnknownError, error);
            master.CurrentPage = (Json) partial;
            return master;
        }
    }

    /// <summary>
    /// Thrown when a client does something that is forbidden
    /// </summary>
    public class ForbiddenException : RESTarException
    {
        internal ForbiddenException(ErrorCodes code, string message) : base(code, message) => Response = Forbidden();
    }

    /// <summary>
    /// Thrown when no HTML was found for a resource view.
    /// </summary>
    public class NoHtmlException : RESTarException
    {
        internal NoHtmlException(IResource resource, string matcher) : base(NoMatchingHtml,
            $"No matching HTML file found for resource '{resource.Name}'. Add a HTML file " +
            $"'{matcher}' to the 'wwwroot/resources' directory.")
        {
        }
    }

    /// <summary>
    /// Thrown when a syntax error was discovered when parsing a request
    /// </summary>
    public class SyntaxException : RESTarException
    {
        internal SyntaxException(ErrorCodes errorCode, string message) : base(errorCode,
            "Syntax error while parsing request: " + message) => Response = BadRequest(this);
    }

    /// <summary>
    /// Thrown when a provided operator was invalid
    /// </summary>
    public class OperatorException : SyntaxException
    {
        internal OperatorException(string c) : base(InvalidConditionOperatorError,
            $"Invalid or missing operator for condition '{c}'. The pr" +
            "esence of one (and only one) operator is required per co" +
            "ndition. Make sure to URI encode all equals (\'=\' to \'" +
            "%3D\') and exclamation marks (\'!\' to \'%21\') in reque" +
            "st URI value literals, to avoid capture. Accepted operat" +
            "ors: " + string.Join(", ", Operator.AvailableOperators))
        {
        }
    }

    /// <summary>
    /// Thrown when a provided operator was forbidden for the given property
    /// </summary>
    public class ForbiddenOperatorException : RESTarException
    {
        internal ForbiddenOperatorException(string c, IResource resource, Operator found, PropertyChain chain,
            IEnumerable<Operator> allowed) : base(InvalidConditionOperatorError,
            $"Invalid operator for condition '{c}'. Operator '{found}' is not allowed when " +
            $"comparing against '{chain.Key}' in resource '{resource.Name}'. Allowed operators" +
            $": {string.Join(", ", allowed.Select(a => $"'{a.Common}'"))}") => Response = BadRequest(this);
    }

    /// <summary>
    /// Thrown when RESTar encounters an error getting entities from an
    /// external data source.
    /// </summary>
    public class SourceException : RESTarException
    {
        internal SourceException(string uri, string message) : base(InvalidSourceDataError,
            $"RESTar could not get entities from source at '{uri}'. {message}") => Response = BadRequest(this);
    }

    /// <summary>
    /// Thrown when an invalid number of data entities was provided for a certain
    /// method.
    /// </summary>
    public class InvalidInputCountException : RESTarException
    {
        internal InvalidInputCountException(RESTarMethods method) : base(DataSourceFormatError,
            $"Invalid input count for method {method:G}. Expected object/row, but found array/multiple rows. " +
            "Only POST accepts multiple objects/rows as input.") => Response = BadRequest(this);
    }

    /// <summary>
    /// Thrown when RESTar cannot locate a resource using a given search string
    /// </summary>
    public class UnknownResourceException : RESTarException
    {
        internal UnknownResourceException(string searchString) : base(UnknownResourceError,
            $"RESTar could not locate any resource by '{searchString}'. To enumerate available " +
            $"resources, GET: {_ResourcesPath} . ") => Response = NotFound(this);
    }

    /// <summary>
    /// Thrown when RESTar encounters an error reading Excel data
    /// </summary>
    public class ExcelInputException : RESTarException
    {
        internal ExcelInputException() : base(ExcelReaderError,
            "There was a format error in the excel input. Check that the file is being transmitted properly. In " +
            "curl, make sure the flag '--data-binary' is used and not '--data' or '-d'") => Response = BadRequest(this);
    }

    /// <summary>
    /// Thrown when RESTar encounters an error writing to the Excel format
    /// </summary>
    public class ExcelFormatException : RESTarException
    {
        internal ExcelFormatException() : base(ExcelReaderError,
            "RESTar was unable to write a query response to an Excel table due to a format error. " +
            "This is likely due to the serializer trying to push an array of heterogeneous objects " +
            "onto a single table, or that some object contains an inner object.") => Response = BadRequest(this);
    }

    /// <summary>
    /// Thrown when RESTar cannot find a property/column in a given resource by a 
    /// given property name.
    /// </summary>
    public class UnknownColumnException : RESTarException
    {
        internal UnknownColumnException(Type resource, string str) : base(UnknownColumnError,
            $"Could not find any property in resource '{resource.Name}' by '{str}'.") => Response = NotFound(this);
    }

    /// <summary>
    /// Thrown when RESTar expected a unique match for a property/column in a resource, but
    /// found more than one.
    /// </summary>
    public class AmbiguousColumnException : RESTarException
    {
        /// <summary>
        /// The possible candidates found
        /// </summary>
        public readonly IEnumerable<string> Candidates;

        /// <summary>
        /// The search string that was used
        /// </summary>
        public readonly string SearchString;

        internal AmbiguousColumnException(Type resource, string str, IEnumerable<string> cands)
            : base(AmbiguousColumnError, $"Could not uniquely identify a property in resource '{resource.Name}' by " +
                                         $"'{str}'. Candidates: {string.Join(", ", cands)}. ")
        {
            SearchString = str;
            Candidates = cands.ToList();
            Response = AmbiguousColumn(this);
        }
    }

    /// <summary>
    /// Thrown when RESTar expected a unique match for a resource, but
    /// found more than one.
    /// </summary>
    public class AmbiguousResourceException : RESTarException
    {
        /// <summary>
        /// The possible candidates found
        /// </summary>
        public readonly ICollection<string> Candidates;

        /// <summary>
        /// The search string that was used
        /// </summary>
        public readonly string SearchString;

        internal AmbiguousResourceException(string searchString, ICollection<string> candidates)
            : base(AmbiguousResourceError, $"RESTar could not uniquely identify a resource by '{searchString}'. " +
                                           $"Candidates were: {string.Join(", ", candidates)}. ")
        {
            SearchString = searchString;
            Candidates = candidates;
            Response = AmbiguousResource(this);
        }
    }

    /// <summary>
    /// Thrown when RESTar encounters an error selecting entities from 
    /// a given resource.
    /// </summary>
    public class AbortedSelectorException : RESTarException
    {
        internal AbortedSelectorException(Exception ie, IRequest request, string message = null)
            : base(AbortedSelect, message ?? (ie.GetType() == typeof(Newtonsoft.Json.JsonSerializationException) ||
                                              ie.GetType() == typeof(Newtonsoft.Json.JsonReaderException)
                                      ? "JSON serialization error, check JSON syntax"
                                      : ""
                                  ), ie) => Response = AbortedOperation(this, request.Method, request.Resource);
    }

    /// <summary>
    /// Thrown when RESTar encounters an error inserting entities into
    /// a given resource.
    /// </summary>
    public class AbortedInserterException : RESTarException
    {
        internal AbortedInserterException(Exception ie, IRequest request, string message = null)
            : base(AbortedInsert, message ?? (ie.GetType() == typeof(Newtonsoft.Json.JsonSerializationException) ||
                                              ie.GetType() == typeof(Newtonsoft.Json.JsonReaderException)
                                      ? "JSON serialization error, check JSON syntax"
                                      : ""
                                  ), ie) => Response = AbortedOperation(this, request.Method, request.Resource);
    }

    /// <summary>
    /// Thrown when RESTar encounters an error updating entities in
    /// a given resource.
    /// </summary>
    public class AbortedUpdaterException : RESTarException
    {
        internal AbortedUpdaterException(Exception ie, IRequest request, string message = null)
            : base(AbortedUpdate, message ?? (ie.GetType() == typeof(Newtonsoft.Json.JsonSerializationException) ||
                                              ie.GetType() == typeof(Newtonsoft.Json.JsonReaderException)
                                      ? "JSON serialization error, check JSON syntax"
                                      : ""
                                  ), ie) => Response = AbortedOperation(this, request.Method, request.Resource);
    }

    /// <summary>
    /// Thrown when RESTar encounters an error deleting entities from
    /// a given resource.
    /// </summary>
    public class AbortedDeleterException : RESTarException
    {
        internal AbortedDeleterException(Exception ie, IRequest request, string message = null)
            : base(AbortedDelete, message ?? (ie.GetType() == typeof(Newtonsoft.Json.JsonSerializationException) ||
                                              ie.GetType() == typeof(Newtonsoft.Json.JsonReaderException)
                                      ? "JSON serialization error, check JSON syntax"
                                      : ""
                                  ), ie) => Response = AbortedOperation(this, request.Method, request.Resource);
    }

    /// <summary>
    /// Thrown when an entity of a resource declared as IValidatable fails validation
    /// </summary>
    public class ValidatableException : RESTarException
    {
        internal ValidatableException(string message) : base(InvalidResourceEntityError, message)
        {
        }
    }

    /// <summary>
    /// Thrown when a resource cannot be identified when registering an alias.
    /// </summary>
    public class UnknownResourceForAliasException : RESTarException
    {
        internal UnknownResourceForAliasException(string searchString, Type match) : base(UnknownResourceError,
            "Resource alias mappings must be provided with fully qualified resource names. No match " +
            $"for '{searchString}'. {(match != null ? $"Did you mean '{match.FullName}'? " : "")}")
        {
        }
    }

    /// <summary>
    /// Thrown when a uniquely matched entity in a resource was expected for a request,
    /// but multiple was found. 
    /// </summary>
    public class AmbiguousMatchException : RESTarException
    {
        internal AmbiguousMatchException(IResource resource) : base(AmbiguousMatchError,
            $"Expected a uniquely matched entity in resource '{resource.Name}' " +
            "for this request, but matched multiple entities satisfying the given " +
            "conditions. To enable manipulation of multiple matched entities (for " +
            "methods that support this), add 'unsafe=true' to the request's meta-" +
            "conditions. See help article with topic 'unsafe' for more info.")
        {
        }
    }

    /// <summary>
    /// Thrown when an invalid members was detected in a virtual resource declaration.
    /// </summary>
    public class VirtualResourceMemberException : RESTarException
    {
        internal VirtualResourceMemberException(string message)
            : base(VirtualResourceMemberError, message)
        {
        }
    }

    /// <summary>
    /// Thrown when an error was detected in a virtual resource declaration.
    /// </summary>
    public class VirtualResourceDeclarationException : RESTarException
    {
        internal VirtualResourceDeclarationException(string message)
            : base(VirtualResourceDeclarationError, message)
        {
        }
    }

    /// <summary>
    /// Thrown when RESTar has run out of dynamic tables for allocation to new 
    /// dynamic resources.
    /// </summary>
    public class NoAvalailableDynamicTableException : RESTarException
    {
        internal NoAvalailableDynamicTableException() : base(NoAvalailableDynamicTableError,
            "RESTar have no more unallocated dynamic tables. Remove an existing table and try again.")
        {
        }
    }

    /// <summary>
    /// Thrown when a call is made to RESTar before RESTarConfig.Init() has been called.
    /// </summary>
    public class NotInitializedException : RESTarException
    {
        internal NotInitializedException() : base(NotInitialized,
            "A call has been made to RESTar before RESTarConfig.Init() was called. Always " +
            "initialize the RESTar instance before making calls to it.")
        {
        }
    }
}