namespace RESTar
{
    /// <summary>
    /// Entities of resources declared as IValidatable will be validated 
    /// automatically by RESTar on insert and update.
    /// </summary>
    public interface IValidatable
    {
        /// <summary>
        /// The validation method called on insert and update. If invalid, include a reason 
        /// in the out parameter to inform the user of the validation error. Return true if and
        /// only if the entity is valid.
        /// </summary>
        bool IsValid(out string invalidReason);
    }
}