using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RESTar
{
    public class SyntaxException : Exception
    {
        public SyntaxException(string message) : base("Syntax error while parsing request: " + message)
        {
        }
    }

    public class ExternalSourceException : Exception
    {
        public readonly string _Message;
        public readonly string Uri;

        public ExternalSourceException(string uri, string message)
            : base($"RESTar could not get entities from external source at '{uri}'. {message}")
        {
            Uri = uri;
            _Message = message;
        }
    }

    public class InvalidInputCountException : Exception
    {
        public RESTarMethods Method;
        public Type Resource;

        public InvalidInputCountException(Type resource, RESTarMethods method)
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

    public class UnknownResourceForMappingException : Exception
    {
        public UnknownResourceForMappingException(string searchString, Type match)
            : base("Resource mappings must be provided with fully qualified resource names. No match " +
                   $"for '{searchString}'. {(match != null ? $"Did you mean '{match.FullName}'? " : "")}")
        {
        }
    }

    public class ResourceMappingException : Exception
    {
        public ResourceMappingException(string alias)
            : base($"RESTar could not map alias '{alias}' to resource. Alias is already mapped to a " +
                   "resource.")
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
                   $"to a single table, or some object including inner objects.")
        {
        }
    }

    public class UnknownColumnException : Exception
    {
        public readonly Type Resource;
        public readonly string SearchString;

        public UnknownColumnException(Type resource, string searchString)
            : base($"RESTar could not locate any column in resource {resource.FullName} by '{searchString}'. " +
                   $"To enumerate columns in this resource, GET: {Settings._ResourcesPath}/{resource.FullName} . ")
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
            : base($"RESTar could not uniquely identify a column in resource {resource.FullName} by " +
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
        public Type Resource;

        public AmbiguousMatchException(Type resource)
        {
            Resource = resource;
        }
    }

    public class RESTarInternalException : Exception
    {
        public RESTarInternalException(string message)
            : base($"An internal RESTar error has been encountered: {message} . ")
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

    public class VirtualResourceSignatureException : Exception
    {
        public VirtualResourceSignatureException(string message)
            : base(message)
        {
        }
    }

    public class CustomEvaluatorSignatureException : Exception
    {
        public CustomEvaluatorSignatureException(MethodInfo method, Type resource)
            : base($"Error in signature of custom evaluator '{method.Name}' in resource '{resource.FullName}'. " +
                   $"Signature must be of form 'public static Starcounter.Response [GET|POST|PATCH|PUT|DELETE]" +
                   $"(IRequest request)'")
        {
        }
    }

    public class VirtualResourceMemberException : Exception
    {
        public VirtualResourceMemberException(string message)
            : base(message)
        {
        }
    }

    public class InvalidResourceDefinitionException : Exception
    {
        public InvalidResourceDefinitionException(string message)
            : base(message)
        {
        }
    }


    public class AbortedSelectorException : Exception
    {
        public AbortedSelectorException(string message) : base(message)
        {
        }
    }

    public class AbortedInserterException : Exception
    {
        public AbortedInserterException(string message) : base(message)
        {
        }
    }

    public class AbortedUpdaterException : Exception
    {
        public AbortedUpdaterException(string message) : base(message)
        {
        }
    }

    public class AbortedDeleterException : Exception
    {
        public AbortedDeleterException(string message) : base(message)
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