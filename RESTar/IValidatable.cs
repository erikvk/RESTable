namespace RESTar
{
    /// <summary>
    /// IValidatable objects will be validated on insert and update
    /// </summary>
    public interface IValidatable
    {
        /// <summary>
        /// The validation method callsed on insert and update.
        /// If invalid, include a reason in the out parameter.
        /// </summary>
        bool Validate(out string reason);
    }
}