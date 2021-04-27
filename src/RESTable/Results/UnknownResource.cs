using System;
using System.Text.RegularExpressions;

namespace RESTable.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTable cannot locate a resource by some search string
    /// </summary>
    public class UnknownResource : NotFound
    {
        /// <inheritdoc />
        public UnknownResource(string searchString) : base(ErrorCodes.UnknownResource,
            $"RESTable could not locate any resource by '{searchString}'. " +
            (Regex.IsMatch(searchString, @"[^\w\d\.]+") ? " Check request URI syntax." : "")) { }
    }
}