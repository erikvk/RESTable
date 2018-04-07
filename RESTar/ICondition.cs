using RESTar.Reflection.Dynamic;

namespace RESTar
{
    /// <summary>
    /// A non-generic interface for conditions
    /// </summary>
    public interface ICondition
    {
        /// <summary>
        /// The key of the condition
        /// </summary>
        string Key { get; }

        /// <summary>
        /// The term describing the property to compare with
        /// </summary>
        Term Term { get; }

        /// <summary>
        /// Converts a condition to a new target type and (optionally) a new key
        /// </summary>
        Condition<T> Redirect<T>(string newKey = null) where T : class;
    }
}