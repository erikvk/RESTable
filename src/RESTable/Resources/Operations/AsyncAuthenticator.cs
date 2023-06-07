using System.Threading;
using System.Threading.Tasks;
using RESTable.Requests;

namespace RESTable.Resources.Operations;

/// <summary>
///     Authenticates a request
/// </summary>
internal delegate ValueTask<AuthResults> AsyncAuthenticator<T>(IRequest<T> request, CancellationToken cancellationToken) where T : class;
