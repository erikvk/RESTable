namespace RESTar
{
    /// <summary>
    /// RegEx patterns used internally by RESTar
    /// </summary>
    internal struct RegEx
    {
        /// <summary>
        /// Used when extracting the protocol from a URI
        /// </summary>
        internal const string Protocol = @"^(?<proto>-[^\?/\(]*)?(?<key>\([^\)]+\))?(?<tail>.*)";

        /// <summary>
        /// The main URI regex, used when parsing requests
        /// </summary>
        internal const string RESTarRequestUri = @"^\??((?<res>/[^/-]*)|((?<res>/[^/-]*)(?<view>-\w*)))?(?<cond>/[^/]*)?(?<meta>/[^/]*)?/?$";

        /// <summary>
        /// The main URI regex, used when parsing requests
        /// </summary>
        internal const string ODataRequestUri = @"(?<entityset>/[^/\?]*)?(?<options>\?[^/]*)?";

        /// <summary>
        /// Checks API keys for invalid characters. May only contain non-whitespace characters and non-parentheses
        /// </summary>
        internal const string ApiKey = @"^[!-~]+$";

        /// <summary>
        /// The base URI regex, used when validating base uris in RESTarConfig.Init
        /// </summary>
        internal const string BaseUri = @"^/?[\/\w]+$";

        /// <summary>
        /// Matches only letters, numbers and underscores
        /// </summary>
        internal const string LettersNumsAndUs = @"^\w+$";

        /// <summary>
        /// Matches only strings that are valid dynamic resource names
        /// </summary>
        internal const string DynamicResourceName = @"^[a-zA-Z0-9_\.]+$";

        /// <summary>
        /// Matches headers in source and destination header syntax
        /// </summary>
        internal const string RequestHeader = @"\[(?<header>.+):[\s]*(?<value>.+)\]";

        /// <summary>
        /// Used when sending unescaped data through a RESTar view model
        /// </summary>
        internal const string ViewMacro = @"\@RESTar\((?<content>[^\(\)]*)\)";

        /// <summary>
        /// Used in setoperations when mapping object data to function parameters
        /// </summary>
        internal const string MapMacro = @"\$\([^\$\(\)]+\)";

        /// <summary>
        /// Matches unsupported OData operators in URIs
        /// </summary>
        internal const string UnsupportedODataOperatorRegex = @"(/| has | not | cast\(.*\)| mul | div | mod | add | sub | isof | or )";

        /// <summary>
        /// Matches condition literals sorrounded with double quotes
        /// </summary>
        internal const string DoubleQuoteRegex = "^\"(?<content>[^\"]*)\"$";

        /// <summary>
        /// Matches condition literals sorrounded with single quotes
        /// </summary>
        internal const string SingleQuoteRegex = "^\'(?<content>[^\']*)\'$";

        /// <summary>
        /// Used when matching parts of URI conditions
        /// </summary>
        internal const string UriCondition = @"^(?<key>[^\!=<>]*)(?<op>(=|\!=|<=|>=|<|>))(?<val>.*)$";
    }
}