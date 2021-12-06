using System.Threading;
using System.Threading.Tasks;
using RESTable.Requests;

namespace RESTable.Resources.Operations;

/// <summary>
///     Selects a stream and content type for a binary resource
/// </summary>
internal delegate BinaryResult BinarySelector<T>(IRequest<T> request) where T : class;

/// <summary>
///     Selects a stream and content type for a binary resource
/// </summary>
internal delegate ValueTask<BinaryResult> AsyncBinarySelector<T>(IRequest<T> request, CancellationToken cancellationToken) where T : class;