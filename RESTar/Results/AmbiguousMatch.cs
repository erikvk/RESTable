using RESTar.Internal;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when a uniquely matched entity was expected for a request, but multiple was found
    /// </summary>
    internal class SafePostAmbiguousMatch : BadRequest
    {
        internal SafePostAmbiguousMatch(string condition) : base(ErrorCodes.AmbiguousMatch,
            $"As part of the SafePost operation, RESTar ran select with '{condition}', expecting either no " +
            $"or a single entity as result. Found multiple. SafePost can only be used with a unique key" +
            "" +
            "for this operation, but found multiple. " +
            "Manipulating multiple entities is either unsupported or unsafe. Specify additional " +
            "conditions or use the 'unsafe' meta-condition")
        { }
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when a uniquely matched entity was expected for a request, but multiple was found
    /// </summary>
    internal class AmbiguousMatch : BadRequest
    {
        internal AmbiguousMatch() : base(ErrorCodes.AmbiguousMatch,
            "Expected a uniquely matched entity for this operation, but found multiple. " +
            "Manipulating multiple entities is either unsupported or unsafe. Specify additional " +
            "conditions or use the 'unsafe' meta-condition") { }
    }
}