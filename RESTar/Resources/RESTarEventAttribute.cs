using System;

namespace RESTar.Resources
{
    /// <inheritdoc />
    /// <summary>
    /// Registers a class as a RESTar event type
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class RESTarEventAttribute : Attribute
    {
        internal string Description { get; }

        /// <inheritdoc />
        /// <param name="description">A description of this event, and when it's triggered</param>
        public RESTarEventAttribute(string description) => Description = description;
    }
}