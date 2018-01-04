using RESTar.Internal;

namespace RESTar.Results.Fail.BadRequest
{
    internal class AmbiguousMatch : BadRequest
    {
        internal AmbiguousMatch(ITarget resource) : base(ErrorCodes.AmbiguousMatch,
            $"Expected a uniquely matched entity in resource '{resource.FullName}', but found multiple. " +
            "Manipulating multiple entities is either unsupported or unsafe. Specify additional " +
            "conditions or use the 'unsafe' meta-condition") { }
    }
}