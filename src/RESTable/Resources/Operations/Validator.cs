namespace RESTable.Resources.Operations
{
    /// <summary>
    /// Defines the operation of validating an entity resource entity
    /// </summary>
    public delegate bool Validator<in T>(T entity, out string invalidReason) where T : class;
}