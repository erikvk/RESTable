namespace RESTar
{
    public enum RESTarPresets : byte
    {
        /// <summary>
        /// Makes GET available for this resource
        /// </summary>
        ReadOnly,

        /// <summary>
        /// Makes POST and DELETE available for this resource
        /// </summary>
        WriteOnly,

        /// <summary>
        /// Makes GET and PATCH available for this resource
        /// </summary>
        ReadAndUpdate,

        /// <summary>
        /// Makes all methods available for this resource
        /// </summary>
        ReadAndWrite
    }

    public enum RESTarMethods
    {
        GET,
        POST,
        PATCH,
        PUT,
        DELETE
    }

    internal enum RESTarOperations
    {
        Select,
        Insert,
        Update,
        Delete
    }

    public enum RESTarResourceType
    {
        /// <summary>
        /// Created by user, non-persistent - should be initialized. Regular
        /// Starcounter resources.
        /// </summary>
        ScStatic,

        /// <summary>
        /// Created by user, non-persistent - should not be initialized.
        /// Resources that inherit from DDictionary.
        /// </summary>
        ScDynamic,

        /// <summary>
        /// Created by user, non-persistent - no public fields
        /// </summary>
        Virtual,

        /// <summary>
        /// Created by RESTar, DynamicResource 01-64. Persistent resources.
        /// </summary>
        Dynamic
    }

    public enum Operators
    {
        nil,
        EQUALS,
        NOT_EQUALS,
        LESS_THAN,
        GREATER_THAN,
        LESS_THAN_OR_EQUALS,
        GREATER_THAN_OR_EQUALS
    }

    public enum ErrorCode
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

        // 110-119: Column locator errors
        UnknownColumnError = 110,
        AmbiguousColumnError = 111,
        UnknownColumnInGeneratedObjectError = 112,

        // 120-129: Resource registration errors
        VirtualResourceMissingInterfaceImplementationError = 120,
        VirtualResourceMemberError = 121,
        NoAvalailableDynamicTableError = 122,

        // 200-300: Handler errors
        AbortedOperation = 200,
        NotSignedIn = 201,
        NotAuthorized = 202,

        DatabaseError = 300,
    }
}