using RESTable.Requests;

namespace RESTable.Resources.Operations
{
    /// <summary>
    /// Authenticates a request
    /// </summary>
    internal delegate AuthResults Authenticator<T>(IRequest<T> request) where T : class;
}