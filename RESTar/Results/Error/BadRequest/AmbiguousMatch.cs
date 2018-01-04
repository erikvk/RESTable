using RESTar.Internal;

namespace RESTar.Results.Error.BadRequest
{
    internal class AmbiguousMatch : BadRequest
    {
        internal AmbiguousMatch(ITarget resource) : base(ErrorCodes.AmbiguousMatch,
            $"Expected a uniquely matched entity in resource '{resource.Name}', but found multiple. " +
            "Manipulating multiple entities is either unsupported or unsafe. Specify additional " +
            "conditions or use the 'unsafe' meta-condition") { }
    }
}