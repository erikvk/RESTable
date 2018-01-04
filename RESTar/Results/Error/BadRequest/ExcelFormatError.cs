using System;
using RESTar.Internal;

namespace RESTar.Results.Error.BadRequest
{
    internal class ExcelFormatError : BadRequest
    {
        internal ExcelFormatError(string message, Exception ie) : base(ErrorCodes.ExcelReaderError,
            $"RESTar was unable to write entities to excel. {message}. ", ie) { }
    }
}