using System;

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

    public enum RESTarOperations
    {
        Select,
        Insert,
        Update,
        Delete
    }

    internal class TypeAttribute : Attribute
    {
        internal readonly Type Type;

        internal TypeAttribute(Type type)
        {
            Type = type;
        }
    }

    public enum RESTarMetaConditions
    {
        Limit,
        Order_desc,
        Order_asc,
        Unsafe,
        Select,
        Add,
        Rename,
        Dynamic,
        Safepost
    }

    public enum ErrorCode
    {
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
        // 110-119: Column locator errors
        UnknownColumnError = 110,
        AmbiguousColumnError = 111,
        UnknownColumnInGeneratedObjectError = 112,

        AbortedOperation = 200,
        DatabaseError = 300   
    }
}