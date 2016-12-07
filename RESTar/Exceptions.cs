using System;
using System.Collections.Generic;
using System.Reflection;

namespace RESTar
{
    public class SyntaxException : Exception
    {
        public SyntaxException(string message) : base("Syntax error while parsing command: " + message)
        {
        }
    }

    public class ExternalSourceException : Exception
    {
        public readonly string Uri;
        public readonly string _Message;

        public ExternalSourceException(string uri, string message)
            : base($"RESTar could not get entities from external source at '{uri}'. {message}")
        {
            Uri = uri;
            _Message = message;
        }
    }

    public class InvalidInputCountException : Exception
    {
        public Type Resource;
        public RESTarMethods Method;

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
        public readonly string SearchString;
        public readonly Type Resource;

        public UnknownColumnException(Type resource, string searchString)
            : base($"RESTar could not locate any column in resource {resource.FullName} by '{searchString}'. " +
                   $"To enumerate columns in this resource, GET: {Settings._ResourcesPath}/{resource.FullName} . ")
        {
            SearchString = searchString;
            Resource = resource;
        }
    }

    public class AmbiguousColumnException : Exception
    {
        public readonly string SearchString;
        public readonly ICollection<string> Candidates;
        public readonly Type Resource;

        public AmbiguousColumnException(Type resource, string searchString, ICollection<string> candidates)
            : base(
                $"RESTar could not uniquely identify a column in resource {resource.FullName} by '{searchString}'. " +
                $"Candidates were: {string.Join(", ", candidates)}. ")
        {
            SearchString = searchString;
            Candidates = candidates;
            Resource = resource;
        }
    }

    public class AmbiguousResourceException : Exception
    {
        public readonly string SearchString;
        public readonly ICollection<string> Candidates;

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

    public class VirtualResourceMissingMethodException : Exception
    {
        public VirtualResourceMissingMethodException(Type resource, string missingMethodDef)
            : base($"The resource type definition for {resource.FullName} is not decorated with the Starcounter " +
                   $"database attribute, and is therefore considered a virtual resource definition. Virtual resources " +
                   $"must contain static method definitions supporting the enabled RESTar methods for the resource " +
                   $"(see help/topic=\"virtual resources\" for more info). {missingMethodDef}")
        {
        }
    }

    public class VirtualResourceSignatureException : Exception
    {
        public VirtualResourceSignatureException(string message)
            : base(message)
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
}