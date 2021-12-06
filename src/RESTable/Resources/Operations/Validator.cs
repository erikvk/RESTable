using System.Collections.Generic;
using RESTable.Requests;

namespace RESTable.Resources.Operations;

/// <summary>
///     Defines the operation of validating an entity resource entity
/// </summary>
public delegate IEnumerable<InvalidMember> Validator<in T>(T entity, RESTableContext context) where T : class;