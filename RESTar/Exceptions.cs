using System;
using System.Collections.Generic;
using System.Linq;

namespace RESTar
{
    public class SyntaxException : Exception
    {
        public SyntaxException(string message) : base("Syntax error while parsing command: " + message)
        {
        }
    }

    public class UnknownResourceException : Exception
    {
        public readonly string SearchString;
        public readonly ICollection<string> Candidates;

        public UnknownResourceException(string searchString, ICollection<string> candidates)
            : base($"RESTar could not uniquely locate a resource by '{searchString}'. " +
                   $"Candidates were: {string.Join(", ", candidates)}")
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
            : base("An internal RESTar error has been encountered:" + message)
        {
        }
    }
}