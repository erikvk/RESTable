using System.Collections.Generic;
using RESTar.Operations;

namespace RESTar
{
    /// <inheritdoc />
    /// <summary>
    /// Prints a link to the RESTar documentation
    /// </summary>
    [RESTar(Method.GET, Description = "Prints a link to the RESTar documentation")]
    public class Help : ISelector<Help>
    {
        /// <summary>
        /// The URL to the RESTar documentation
        /// </summary>
        public const string DocumentationUrl = "https://github.com/Mopedo/Home/tree/master/RESTar/Consuming%20a%20RESTar%20API";

        /// <summary>
        /// The property holding the documentation URL
        /// </summary>
        public string DocumentationAvailableAt { get; set; }

        /// <inheritdoc />
        public IEnumerable<Help> Select(IRequest<Help> request) => new[] {new Help {DocumentationAvailableAt = DocumentationUrl}};
    }
}