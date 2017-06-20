using System;
using System.Collections.Generic;
using System.Linq;
using RESTar.Internal;
using RESTar.Requests;
using RESTar.View;
using Starcounter;
using static RESTar.Internal.ErrorCodes;
using static RESTar.Requests.Responses;
using static RESTar.Settings;
using static RESTar.View.MessageType;
using IResource = RESTar.Internal.IResource;

namespace RESTar
{
    public class RESTarException : Exception
    {
        public ErrorCodes ErrorCode;
        public Response Response;

        public RESTarException(ErrorCodes code, string message) : base(message) => ErrorCode = code;
        public RESTarException(ErrorCodes code, string message, Exception ie) : base(message, ie) => ErrorCode = code;

        public static Json HandleViewException(Exception e)
        {
            var re = e as RESTarException;
            var master = Self.GET<View.Page>("/__restar/__page");
            var partial = master.CurrentPage as IRESTarView ?? new MessageWindow().Populate();
            partial.SetMessage(e.Message, re?.ErrorCode ?? UnknownError, error);
            master.CurrentPage = (Json) partial;
            return master;
        }
    }

    public class ForbiddenException : RESTarException
    {
        public ForbiddenException(ErrorCodes code, string message) : base(code, message) => Response = Forbidden();
    }

    public class NoHtmlException : RESTarException
    {
        public NoHtmlException(IResource resource, string matcher) : base(NoMatchingHtml,
            $"No matching HTML file found for resource '{resource.Name}'. Add a HTML file " +
            $"'{matcher}' to the 'wwwroot/resources' directory.")
        {
        }
    }

    public class SyntaxException : RESTarException
    {
        public SyntaxException(ErrorCodes errorCode, string message) : base(errorCode,
            "Syntax error while parsing request: " + message) => Response = BadRequest(this);
    }

    public class OperatorException : SyntaxException
    {
        public OperatorException(string c) : base(InvalidConditionOperatorError,
            $"Invalid or missing operator for condition '{c}'. The pr" +
            "esence of one (and only one) operator is required per co" +
            "ndition. Make sure to URI encode all equals (\'=\' to \'" +
            "%3D\') and exclamation marks (\'!\' to \'%21\') in reque" +
            "st URI value literals, to avoid capture. Accepted operat" +
            "ors: " + string.Join(", ", Operator.AvailableOperators))
        {
        }
    }

    public class SourceException : RESTarException
    {
        public SourceException(string uri, string message) : base(InvalidSourceDataError,
            $"RESTar could not get entities from source at '{uri}'. {message}") => Response = BadRequest(this);
    }

    public class InvalidInputCountException : RESTarException
    {
        public InvalidInputCountException(RESTarMethods method) : base(DataSourceFormatError,
            $"Invalid input count for method {method:G}. Expected object/row, but found array/multiple rows. " +
            "Only POST accepts multiple objects/rows as input.") => Response = BadRequest(this);
    }

    public class UnknownResourceException : RESTarException
    {
        public UnknownResourceException(string searchString) : base(UnknownResourceError,
            $"RESTar could not locate any resource by '{searchString}'. To enumerate available " +
            $"resources, GET: {_ResourcesPath} . ") => Response = NotFound(this);
    }

    public class ExcelInputException : RESTarException
    {
        public ExcelInputException() : base(ExcelReaderError,
            "There was a format error in the excel input. Check that the file is being transmitted properly. In " +
            "curl, make sure the flag '--data-binary' is used and not '--data' or '-d'") => Response = BadRequest(this);
    }

    public class ExcelFormatException : RESTarException
    {
        public ExcelFormatException() : base(ExcelReaderError,
            "RESTar was unable to write a query response to an Excel table due to a format error. " +
            "This is likely due to the serializer trying to push an array of heterogeneous objects " +
            "onto a single table, or that some object contains an inner object.") => Response = BadRequest(this);
    }

    public class UnknownColumnException : RESTarException
    {
        public UnknownColumnException(Type resource, string str) : base(UnknownColumnError,
            $"Could not find any property in resource '{resource.Name}' by '{str}'.") => Response = NotFound(this);
    }

    public class AmbiguousColumnException : RESTarException
    {
        public readonly IEnumerable<string> Candidates;
        public readonly string SearchString;

        public AmbiguousColumnException(Type resource, string str, IEnumerable<string> cands)
            : base(AmbiguousColumnError, $"Could not uniquely identify a property in resource '{resource.Name}' by " +
                                         $"'{str}'. Candidates: {string.Join(", ", cands)}. ")
        {
            SearchString = str;
            Candidates = cands.ToList();
            Response = AmbiguousColumn(this);
        }
    }

    public class AmbiguousResourceException : RESTarException
    {
        public readonly ICollection<string> Candidates;
        public readonly string SearchString;

        public AmbiguousResourceException(string searchString, ICollection<string> candidates)
            : base(AmbiguousResourceError, $"RESTar could not uniquely identify a resource by '{searchString}'. " +
                                           $"Candidates were: {string.Join(", ", candidates)}. ")
        {
            SearchString = searchString;
            Candidates = candidates;
            Response = AmbiguousResource(this);
        }
    }

    public class AbortedSelectorException : RESTarException
    {
        internal AbortedSelectorException(Exception ie, Requests.Request request, string message = null)
            : base(AbortedSelect, message ?? (ie.GetType() == typeof(Newtonsoft.Json.JsonSerializationException) ||
                                              ie.GetType() == typeof(Newtonsoft.Json.JsonReaderException)
                                      ? "JSON serialization error, check JSON syntax"
                                      : $"An exception of type {ie.GetType().FullName} was thrown"
                                  ), ie) => Response = AbortedOperation(this, request.Method, request.Resource);
    }

    public class AbortedInserterException : RESTarException
    {
        internal AbortedInserterException(Exception ie, Requests.Request request, string message = null)
            : base(AbortedInsert, message ?? (ie.GetType() == typeof(Newtonsoft.Json.JsonSerializationException) ||
                                              ie.GetType() == typeof(Newtonsoft.Json.JsonReaderException)
                                      ? "JSON serialization error, check JSON syntax"
                                      : $"An exception of type {ie.GetType().FullName} was thrown"
                                  ), ie) => Response = AbortedOperation(this, request.Method, request.Resource);
    }

    public class AbortedUpdaterException : RESTarException
    {
        internal AbortedUpdaterException(Exception ie, Requests.Request request, string message = null)
            : base(AbortedUpdate, message ?? (ie.GetType() == typeof(Newtonsoft.Json.JsonSerializationException) ||
                                              ie.GetType() == typeof(Newtonsoft.Json.JsonReaderException)
                                      ? "JSON serialization error, check JSON syntax"
                                      : $"An exception of type {ie.GetType().FullName} was thrown"
                                  ), ie) => Response = AbortedOperation(this, request.Method, request.Resource);
    }

    public class AbortedDeleterException : RESTarException
    {
        internal AbortedDeleterException(Exception ie, Requests.Request request, string message = null)
            : base(AbortedDelete, message ?? (ie.GetType() == typeof(Newtonsoft.Json.JsonSerializationException) ||
                                              ie.GetType() == typeof(Newtonsoft.Json.JsonReaderException)
                                      ? "JSON serialization error, check JSON syntax"
                                      : $"An exception of type {ie.GetType().FullName} was thrown"
                                  ), ie) => Response = AbortedOperation(this, request.Method, request.Resource);
    }

    public class NoContentException : Exception
    {
    }

    public class ValidatableException : RESTarException
    {
        public ValidatableException(string message) : base(InvalidResourceEntityError, message)
        {
        }
    }

    public class UnknownResourceForAliasException : RESTarException
    {
        public UnknownResourceForAliasException(string searchString, Type match) : base(UnknownResourceError,
            "Resource alias mappings must be provided with fully qualified resource names. No match " +
            $"for '{searchString}'. {(match != null ? $"Did you mean '{match.FullName}'? " : "")}")
        {
        }
    }

    public class AmbiguousMatchException : RESTarException
    {
        public AmbiguousMatchException(IResource resource) : base(AmbiguousMatchError,
            $"Expected a uniquely matched entity in resource '{resource.Name}' " +
            "for this request, but matched multiple entities satisfying the given " +
            "conditions. To enable manipulation of multiple matched entities (for " +
            "methods that support this), add 'unsafe=true' to the request's meta-" +
            "conditions. See help article with topic 'unsafe' for more info.")
        {
        }
    }

    public class VirtualResourceMissingInterfaceImplementation : RESTarException
    {
        public VirtualResourceMissingInterfaceImplementation(Type resource, string _interface)
            : base(VirtualResourceMissingInterfaceImplementationError,
                $"The virtual resource {resource.FullName} does not implement the interfaces necessary " +
                $"for it to work as a RESTar resource. Assigned methods require an implementation of " +
                $"{_interface} (see the help article on virtual resources for more info)")
        {
        }
    }

    public class VirtualResourceMemberException : RESTarException
    {
        public VirtualResourceMemberException(string message)
            : base(VirtualResourceMemberError, message)
        {
        }
    }

    public class NoAvalailableDynamicTableException : RESTarException
    {
        public NoAvalailableDynamicTableException() : base(NoAvalailableDynamicTableError,
            "RESTar have no more unallocated dynamic tables. " +
            "Remove an existing table and try again.")
        {
        }
    }
}