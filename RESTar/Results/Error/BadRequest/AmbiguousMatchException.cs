using RESTar.Internal;

namespace RESTar.Results.Error.BadRequest
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when a uniquely matched entity in a resource was expected for a request,
    /// but multiple was found. 
    /// </summary>
    public class AmbiguousMatchException : Base
    {
        internal AmbiguousMatchException(ITarget resource) : base(ErrorCodes.AmbiguousMatch,
            $"Expected a uniquely matched entity in resource '{resource.Name}', but found multiple. " +
            "Manipulating multiple entities is either unsupported or unsafe. Specify additional " +
            "conditions or use the 'unsafe' meta-condition") { }
    }
}