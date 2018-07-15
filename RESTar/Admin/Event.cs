using RESTar.Resources;
using Starcounter;

namespace RESTar.Admin
{
    /// <summary>
    /// Holds all events that are registered for this RESTar instance
    /// </summary>
    [Database, RESTar(Method.GET, Description = description)]
    public class Event
    {
        internal const string All = "SELECT t FROM RESTar.Admin.Event t";
        internal const string ByName = All + " WHERE t.Name =?";

        private const string description = "The events that are available for this RESTar instance. " +
                                           "Events can, for example, be used to trigger Webhooks";

        /// <summary>
        /// The name of the event
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// A description of the event, and when it's raised
        /// </summary>
        public string Description { get; }

        internal Event(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }
}