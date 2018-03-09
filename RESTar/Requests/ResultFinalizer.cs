using RESTar.Operations;

namespace RESTar.Requests
{
    /// <summary>
    /// The Finalizer finalizes a result according to some protocol
    /// </summary>
    public delegate IFinalizedResult ResultFinalizer(IResult result, IContentTypeProvider contentTypeProvider);
}