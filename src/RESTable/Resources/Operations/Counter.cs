using RESTable.Requests;

namespace RESTable.Resources.Operations;

/// <summary>
///     Counts the entities that satisfy certain conditions provided in the request
/// </summary>
/// <typeparam name="T">The resource type</typeparam>
internal delegate long Counter<T>(IRequest<T> request) where T : class;