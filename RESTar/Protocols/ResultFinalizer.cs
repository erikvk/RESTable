using RESTar.Operations;

namespace RESTar.Protocols
{
    /// <summary>
    /// The Finalizer finalizes a result according to some protocol
    /// </summary>
    internal delegate IFinalizedResult ResultFinalizer(Result result);
}