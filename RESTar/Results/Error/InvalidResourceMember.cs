using RESTar.Internal;

namespace RESTar.Results.Error
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an invalid member inside a resource declaration
    /// </summary>
    public class InvalidResourceMember : BadRequest.BadRequest
    {
        internal InvalidResourceMember(string message) : base(ErrorCodes.InvalidResourceMember, message) { }
    }
}