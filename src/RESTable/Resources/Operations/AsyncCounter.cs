using System.Threading;
using System.Threading.Tasks;
using RESTable.Requests;

namespace RESTable.Resources.Operations;

/// <summary>
///     Counts the entities that satisfy certain conditions provided in the request
/// </summary>
/// <typeparam name="T">The resource type</typeparam>
internal delegate ValueTask<long> AsyncCounter<T>(IRequest<T> request, CancellationToken cancellationToken) where T : class;
