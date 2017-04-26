using System;
using System.Collections.Generic;
using System.Reflection;
using RESTar.Internal;

namespace RESTar
{
    public class ForbiddenException : Exception
    {
    }

    public class ValidatableException : Exception
    {
        public ValidatableException(string message) : base(message)
        {
        }
    }

    public class SyntaxException : Exception
    {
        internal ErrorCode errorCode;

        public SyntaxException(string message, ErrorCode errorCode)
            : base("Syntax error while parsing request: " + message)
        {
        }
    }

    public class OperatorException : SyntaxException
    {
        public OperatorException(string c)
            : base($"Invalid or missing operator for condition '{c}'. The pr" +
                   "esence of one (and only one) operator is required per co" +
                   "ndition. Make sure to URI encode all equals (\'=\' to \'" +
                   "%3D\') and exclamation marks (\'!\' to \'%21\') in reque" +
                   "st URI value literals, to avoid capture. Accepted operat" +
                   "ors: " + string.Join(", ", Operator.AvailableOperators),
                ErrorCode.InvalidConditionOperatorError)
        {
        }
    }

    public class SourceException : Exception
    {
        public readonly string _Message;
        public readonly string Uri;

        public SourceException(string uri, string message)
            : base($"RESTar could not get entities from source at '{uri}'. {message}")
        {
            Uri = uri;
            _Message = message;
        }
    }

    public class InvalidInputCountException : Exception
    {
        public RESTarMethods Method;
        public IResource Resource;

        public InvalidInputCountException(IResource resource, RESTarMethods method)
            : base($"Invalid input count for method {method:G}. Expected object/row, but found array/multiple rows. " +
                   $"Only POST accepts multiple objects/rows as input.")
        {
            Resource = resource;
            Method = method;
        }
    }

    public class UnknownResourceException : Exception
    {
        public readonly string SearchString;

        public UnknownResourceException(string searchString)
            : base($"RESTar could not locate any resource by '{searchString}'. To enumerate available " +
                   $"resources, GET: {Settings._ResourcesPath} . ")
        {
            SearchString = searchString;
        }
    }

    public class UnknownResourceForAliasException : Exception
    {
        public UnknownResourceForAliasException(string searchString, Type match)
            : base("Resource alias mappings must be provided with fully qualified resource names. No match " +
                   $"for '{searchString}'. {(match != null ? $"Did you mean '{match.FullName}'? " : "")}")
        {
        }
    }

    public class ExcelInputException : Exception
    {
        public ExcelInputException()
            : base("There was a format error in the excel input. Check that the file is being transmitted " +
                   "properly. In curl, make sure the flag '--data-binary' is used and not '--data' or '-d'")
        {
        }
    }

    public class ExcelFormatException : Exception
    {
        public ExcelFormatException()
            : base($"RESTar was unable to write a query response to an Excel table due to a format error. " +
                   $"This is likely due to the serializer trying to push an array of heterogeneous objects " +
                   $"onto a single table, or that some object contains an inner object.")
        {
        }
    }

    public class UnknownColumnException : Exception
    {
        public readonly Type Resource;
        public readonly string SearchString;

        public UnknownColumnException(Type resource, string searchString)
            : base($"RESTar could not locate any column in resource {resource.Name} by '{searchString}'.")
        {
            SearchString = searchString;
            Resource = resource;
        }
    }

    public class CustomEntityUnknownColumnException : Exception
    {
        public readonly string SearchString;

        public CustomEntityUnknownColumnException(string searchString, string jsonRepresentation)
            : base($"RESTar could not locate any column by '{searchString}' when working with the selected entity:" +
                   $"\n{jsonRepresentation}. ")
        {
            SearchString = searchString;
        }
    }

    public class AmbiguousColumnException : Exception
    {
        public readonly ICollection<string> Candidates;
        public readonly Type Resource;
        public readonly string SearchString;

        public AmbiguousColumnException(Type resource, string searchString, ICollection<string> candidates)
            : base($"RESTar could not uniquely identify a column in resource {resource.Name} by " +
                   $"'{searchString}'. Candidates were: {string.Join(", ", candidates)}. ")
        {
            SearchString = searchString;
            Candidates = candidates;
            Resource = resource;
        }
    }

    public class AmbiguousResourceException : Exception
    {
        public readonly ICollection<string> Candidates;
        public readonly string SearchString;

        public AmbiguousResourceException(string searchString, ICollection<string> candidates)
            : base($"RESTar could not uniquely identify a resource by '{searchString}'. " +
                   $"Candidates were: {string.Join(", ", candidates)}. ")
        {
            SearchString = searchString;
            Candidates = candidates;
        }
    }

    public class AmbiguousMatchException : Exception
    {
        public AmbiguousMatchException(IResource resource)
            : base(
                $"Expected a uniquely matched entity in resource '{resource.Name}' " +
                "for this request, but matched multiple entities satisfying the given " +
                "conditions. To enable manipulation of multiple matched entities (for " +
                "methods that support this), add 'unsafe=true' to the request's meta-" +
                "conditions. See help article with topic 'unsafe' for more info."
            )
        {
        }
    }

    public class VirtualResourceMissingInterfaceImplementation : Exception
    {
        public VirtualResourceMissingInterfaceImplementation(Type resource, string _interface)
            : base($"The virtual resource {resource.FullName} does not implement the interfaces necessary " +
                   $"for it to work as a RESTar resource. Assigned methods require an implementation of " +
                   $"{_interface} (see the help article on virtual resources for more info)")
        {
        }
    }

    public class NoContentException : Exception
    {
    }

    public class VirtualResourceMemberException : Exception
    {
        public VirtualResourceMemberException(string message)
            : base(message)
        {
        }
    }

    public class AbortedSelectorException : Exception
    {
        public AbortedSelectorException(Exception innerException, string message = null)
            : base(message ??
                   (innerException.GetType() == typeof(Jil.DeserializationException) ||
                    innerException.GetType() == typeof(Newtonsoft.Json.JsonSerializationException) ||
                    innerException.GetType() == typeof(Newtonsoft.Json.JsonReaderException)
                       ? "JSON serialization error, check JSON syntax"
                       : $"An exception of type {innerException.GetType().FullName} was thrown"), innerException)
        {
        }
    }

    public class AbortedInserterException : Exception
    {
        public AbortedInserterException(Exception innerException, string message = null)
            : base(message ??
                   (innerException.GetType() == typeof(Jil.DeserializationException) ||
                    innerException.GetType() == typeof(Newtonsoft.Json.JsonSerializationException) ||
                    innerException.GetType() == typeof(Newtonsoft.Json.JsonReaderException)
                       ? "JSON serialization error, check JSON syntax"
                       : $"An exception of type {innerException.GetType().FullName} was thrown"), innerException)
        {
        }
    }

    public class AbortedUpdaterException : Exception
    {
        public AbortedUpdaterException(Exception innerException, string message = null)
            : base(message ??
                   (innerException.GetType() == typeof(Jil.DeserializationException) ||
                    innerException.GetType() == typeof(Newtonsoft.Json.JsonSerializationException) ||
                    innerException.GetType() == typeof(Newtonsoft.Json.JsonReaderException)
                       ? "JSON serialization error, check JSON syntax"
                       : $"An exception of type {innerException.GetType().FullName} was thrown"), innerException)
        {
        }
    }

    public class AbortedDeleterException : Exception
    {
        public AbortedDeleterException(Exception innerException, string message = null)
            : base(message ??
                   (innerException.GetType() == typeof(Jil.DeserializationException) ||
                    innerException.GetType() == typeof(Newtonsoft.Json.JsonSerializationException) ||
                    innerException.GetType() == typeof(Newtonsoft.Json.JsonReaderException)
                       ? "JSON serialization error, check JSON syntax"
                       : $"An exception of type {innerException.GetType().FullName} was thrown"), innerException)
        {
        }
    }

    public class NoAvalailableDynamicTableException : Exception
    {
        public NoAvalailableDynamicTableException() : base("RESTar have no more unallocated dynamic tables. " +
                                                           "Remove an existing table and try again.")
        {
        }
    }
}