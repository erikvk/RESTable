using RESTar.Internal;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters a null value for the 'uri' parameter in a call to RequestEvaluator.Evaluate
    /// </summary>
    public class MissingUri : Error
    {
        internal MissingUri() : base(ErrorCodes.MissingUri, "The 'uri' parameter was null in a call to RequestEvaluator.Evaluate") { }
    }
}