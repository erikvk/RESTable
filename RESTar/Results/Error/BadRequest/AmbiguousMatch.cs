using RESTar.Internal;

namespace RESTar.Results.Error.BadRequest
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when a uniquely matched entity was expected for a request, but multiple was found
    /// </summary>
    public class AmbiguousMatch : BadRequest
    {
        internal AmbiguousMatch() : base(ErrorCodes.AmbiguousMatch,
            "Expected a uniquely matched entity for this operation, but found multiple. " +
            "Manipulating multiple entities is either unsupported or unsafe. Specify additional " +
            "conditions or use the 'unsafe' meta-condition") { }
    }
}