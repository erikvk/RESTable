
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
        UnknownError = 000,

        // - - - - - - - - - 
        // 001-099: Request errors
        // 000-009: URI errors
        UnknownMetaConditionError = 001,
        InvalidMetaConditionValueTypeError = 002,
        InvalidMetaConditionOperatorError = 003,
        InvalidMetaConditionSyntaxError = 004,
        InvalidMetaConditionKey = 005,
        InvalidConditionSyntaxError = 006,
        InvalidConditionOperatorError = 007,
        InvalidSeparatorCount = 009,

        // 010-019: Data source syntax and format errors
        JsonDeserializationError = 010,
        ExcelReaderError = 011,
        DataSourceFormatError = 012,
        NoDataSourceError = 013,
        UnsupportedContentType = 014,

        // 020-029: Headers error
        InvalidSourceDataError = 020,
        InvalidSourceFormatError = 021,
        InvalidDestinationError = 022,

        // - - - - - - - - - 
        // 100-199: Resource errors
        // 100-109: Resource locator errors
        UnknownResourceError = 100,
        UnknownResourceForMappingError = 101,
        AmbiguousResourceError = 102,
        AmbiguousMatchError = 103,
        InvalidResourceEntityError = 104,

        // 110-119: Property locator errors
        UnknownPropertyError = 110,
        AmbiguousPropertyError = 111,
        UnknownPropertyOfGeneratedObjectError = 112,

        // 120-129: Resource registration errors
        VirtualResourceMissingInterfaceImplementationError = 120,
        VirtualResourceMemberError = 121,
        NoAvalailableDynamicTableError = 122,
        VirtualResourceDeclarationError = 123,

        // 130-139: Alias errors
        AliasAlreadyInUseError = 124,

        // 200-399: Handler errors
        AbortedSelect = 201,
        AbortedInsert = 202,
        AbortedUpdate = 203,
        AbortedDelete = 204,
        NotSignedIn = 210,
        NotAuthorized = 211,
        NoMatchingHtml = 212,

        DatabaseError = 300,
        NotInitialized = 400,
    }
}