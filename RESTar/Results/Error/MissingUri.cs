using RESTar.Internal;

namespace RESTar.Results.Error
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters a null value for the 'uri' parameter in a call to RequestEvaluator.Evaluate
    /// </summary>
    public class MissingUri : RESTarError
    {
        internal MissingUri() : base(ErrorCodes.MissingUri, "The 'uri' parameter was null in a call to RequestEvaluator.Evaluate") { }
    }
}