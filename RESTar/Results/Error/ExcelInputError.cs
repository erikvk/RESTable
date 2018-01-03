using RESTar.Internal;

namespace RESTar.Results.Error
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an error reading Excel data
    /// </summary>
    public class ExcelInputError : BadRequest
    {
        internal ExcelInputError(string message) : base(ErrorCodes.ExcelReaderError,
            "There was a format error in the excel input. Check that the file is being transmitted properly. In " +
            "curl, make sure the flag '--data-binary' is used and not '--data' or '-d'. Message: " + message) { }
    }
}