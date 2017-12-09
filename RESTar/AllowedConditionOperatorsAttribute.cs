using System;
using System.Linq;

namespace RESTar {
    /// <inheritdoc />
    /// <summary>
    /// An attribute that can be used to decorate field and property declarations, and assign
    /// allowed operators for use in conditions that reference them.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    [Obsolete("Use RESTarMemberAttribute instead")]
    public class AllowedConditionOperatorsAttribute : Attribute
    {
        /// <summary>
        /// Only these operators will be allowed in conditions targeting this property
        /// </summary>
        public Operator[] Operators { get; set; }

        /// <inheritdoc />
        /// <summary>
        /// Creates a new instance ot the AllowedOperators attribute, using the 
        /// provided list of strings to parse allowed operators.
        /// </summary>
        /// <param name="allowedOperators"></param>
        public AllowedConditionOperatorsAttribute(params string[] allowedOperators)
        {
            // NOTE: params Operator[] is not a valid constructor parameter in C#
            try
            {
                Operators = allowedOperators.Select(a => (Operator) a).ToArray();
            }
            catch
            {
                throw new Exception("Invalid RESTarMemberAttribute declaration. Invalid operator string in " +
                                    "allowedOperators.");
            }
        }
    }
}