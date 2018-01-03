using RESTar.Internal;

namespace RESTar.Results.Error
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when an invalid members was detected in a resource declaration.
    /// </summary>
    public class ResourceMemberException : BadRequest
    {
        internal ResourceMemberException(string message) : base(ErrorCodes.InvalidResourceMember, message) { }
    }
}