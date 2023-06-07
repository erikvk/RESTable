using System.Collections.Generic;
using RESTable.Requests;

namespace RESTable.Resources.Operations;

/// <summary>
///     Specifies the Select operation used in GET from a view. Select gets a set
///     of entities from a resource that satisfy certain conditions provided in the request,
///     and returns them.
/// </summary>
/// <typeparam name="T">The resource type</typeparam>
internal delegate IEnumerable<T> ViewSelector<T>(IRequest<T> request) where T : class;
