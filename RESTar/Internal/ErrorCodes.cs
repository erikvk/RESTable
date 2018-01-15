#pragma warning disable 1591
namespace RESTar.Internal
{
    /// <summary>
    /// The error codes used by RESTar
    /// </summary>
    public enum ErrorCodes
    {
        NoError = -1,

        // 000: Unknown error
        Unknown = 000,

        // - - - - - - - - - 
        // 001-099: Request errors
        // 000-009: URI errors
        InvalidUriSyntax = 001,
        InvalidMetaConditionValueType = 002,
        InvalidMetaConditionOperator = 003,
        InvalidMetaConditionSyntax = 004,
        InvalidMetaConditionKey = 005,
        InvalidConditionSyntax = 006,
        InvalidConditionOperator = 007,
        InvalidResourceSpecifier = 008,
        InvalidSeparator = 009,

        // 010-019: Data source syntax and format errors
        FailedJsonDeserialization = 010,
        ExcelReaderError = 011,
        DataSourceFormat = 012,
        NoDataSource = 013,
        UnsupportedContent = 014,
        UnknownFormatter = 015,
        NotAcceptable = 016,

        // 020-029: Headers error
        InvalidSourceData = 020,
        InvalidSource = 021,
        InvalidDestination = 022,

        // - - - - - - - - - 
        // 100-199: Resource errors
        // 100-109: Resource locator errors
        UnknownResource = 100,
        UnknownResourceForMapping = 101,
        AmbiguousResource = 102,
        AmbiguousMatch = 103,
        InvalidResourceEntity = 104,
        ResourceIsInternal = 105,
        UnknownResourceView = 106,
        UnknownMacro = 107,

        // 110-119: Property locator errors
        UnknownProperty = 110,
        AmbiguousProperty = 111,
        UnknownPropertyOfGeneratedObject = 112,

        // 120-129: Resource registration errors
        VirtualResourceMissingInterfaceImplementation = 120,
        InvalidResourceMember = 121,
        NoAvalailableDynamicTable = 122,
        InvalidVirtualResourceDeclaration = 123,
        InvalidResourceDeclaration = 124,
        InvalidResourceViewDeclaration = 125,
        InvalidTerminalDeclaration = 126,


        // 130-139: Alias errors
        AliasAlreadyInUse = 130,
        AliasEqualToResourceName = 131,

        // 200-399: Handler errors
        AbortedSelect = 201,
        AbortedInsert = 202,
        AbortedUpdate = 203,
        AbortedDelete = 204,
        AbortedCount = 205,
        NotSignedIn = 210,
        NotAuthorized = 211,
        NoMatchingHtml = 212,
        FailedResourceAuthentication = 213,

        DatabaseError = 300,
        AbortedByCommitHook = 301,
        NotInitialized = 400,
        AddOnError = 401,
        ResourceProviderError = 402,
        ResourceWrapperError = 403,
        InfiniteLoopDetected = 404,
        NotImplemented = 405,
        UnsupportedODataProtocolVersion = 406,
        MissingConfigurationFile = 407,
    }
}