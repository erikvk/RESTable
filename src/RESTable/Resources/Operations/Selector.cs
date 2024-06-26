using System.Collections.Generic;
using RESTable.Requests;

namespace RESTable.Resources.Operations;

/// <summary>
///     Specifies the Select operation used in GET, PATCH, PUT and DELETE. Select gets a set
///     and returns them.
/// </summary>
/// <typeparam name="T">The resource type</typeparam>
internal delegate IEnumerable<T> Selector<T>(IRequest<T> request) where T : class;
