using RESTar.Admin;

namespace RESTar.Operations
{
    /// <inheritdoc />
    /// <summary>
    /// Interface used to register a Profiler for a given resource type
    /// </summary>
    public interface IProfiler<T> : IOperationsInterface where T : class
    {
        /// <summary>
        /// The delete method for this IDeleter instance. Defines the Delete
        /// operation for a given resource.
        /// </summary>
        ResourceProfile Profile(IRequest<T> request);
    }
}