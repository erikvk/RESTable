using System;
using RESTar.Internal;

namespace RESTar.Results.Error.BadRequest
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an error writing to the Excel format
    /// </summary>
    public class ExcelFormatError : Base
    {
        internal ExcelFormatError(string message, Exception ie) : base(ErrorCodes.ExcelReaderError,
            $"RESTar was unable to write entities to excel. {message}. ", ie) { }
    }
}