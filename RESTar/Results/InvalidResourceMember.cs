using RESTar.Internal;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an invalid member inside a resource declaration
    /// </summary>
    public class InvalidResourceMember : BadRequest
    {
        internal InvalidResourceMember(string message) : base(ErrorCodes.InvalidResourceMember, message) { }
    }
}