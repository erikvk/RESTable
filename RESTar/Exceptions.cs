﻿using System;
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

        public UnknownResourceException(string searchString)
            : base($"RESTar could not locate any resource by '{searchString}'. To enumerate available " +
                   $"resources, GET: {Settings._ResourcesPath} . ")
        {
            SearchString = searchString;
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
            : base($"RESTar could not uniquely locate a column in resource {resource.FullName} by '{searchString}'. " +
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
            : base($"RESTar could not uniquely locate a resource by '{searchString}'. " +
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
}