
namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// A 200 result that encodes a content
    /// </summary>
    public abstract class Content : OK
    {
        /// <inheritdoc />
        protected Content(IRequest request) : base(request) { }
    }
}