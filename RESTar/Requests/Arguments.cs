using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using RESTar.Admin;
using RESTar.Protocols;
using static System.Text.RegularExpressions.RegexOptions;
using IResource = RESTar.Internal.IResource;

namespace RESTar.Requests
{
    /// <summary>
    /// A RESTar class that defines the arguments that are used when creating a RESTar request to evaluate 
    /// an incoming call. Arguments is a unified way to talk about the input to request evaluation, 
    /// regardless of protocol and web technologies.
    /// </summary>
    internal class Arguments
    {
        internal string ResourceSpecifier { get; set; }
        internal string ViewName { get; set; }
        internal List<UriCondition> UriConditions { get; }
        internal List<UriCondition> UriMetaConditions { get; }
        internal IResource IResource => Resource.Find(ResourceSpecifier);
        internal DbMacro Macro { get; set; }
        internal Origin Origin { get; set; }
        internal byte[] BodyBytes { get; set; }
        internal IDictionary<string, string> Headers { get; set; }
        internal string ContentType { get; set; }
        internal string Accept { get; set; }
        internal ResultFinalizer ResultFinalizer { get; set; }
        private static readonly string DefaultResourceSpecifier = typeof(AvailableResource).FullName;

        internal IEnumerable<KeyValuePair<string, string>> CustomHeaders => Headers.Where(h =>
            !Regex.IsMatch(h.Key, RegEx.ReservedHeaders, IgnoreCase));

        internal string UriString
        {
            get
            {
                using (var writer = new StringWriter())
                {
                    writer.Write('/');
                    writer.Write(Macro != null ? '$' + Macro.Name : ResourceSpecifier);
                    writer.Write('/');
                    writer.Write(UriConditions != null ? string.Join("$", UriConditions) : null);
                    writer.Write('/');
                    writer.Write(UriMetaConditions != null ? string.Join("$", UriMetaConditions) : null);
                    return writer.ToString().TrimEnd('/');
                }
            }
        }

        internal Arguments()
        {
            UriConditions = new List<UriCondition>();
            UriMetaConditions = new List<UriCondition>();
            ResourceSpecifier = DefaultResourceSpecifier;
        }
    }
}