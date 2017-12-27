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
    /// Contains parameters for a RESTar URI
    /// </summary>
    public interface IUriParameters
    {
        string ResourceSpecifier { get; }
        string ViewName { get; }
        List<UriCondition> UriConditions { get; }
        List<UriCondition> UriMetaConditions { get; }
    }

    /// <summary>
    /// A RESTar class that defines the arguments that are used when creating a RESTar request to evaluate 
    /// an incoming call. Arguments is a unified way to talk about the input to request evaluation, 
    /// regardless of protocol and web technologies.
    /// </summary>
    internal class Arguments : IUriParameters
    {
        public string ResourceSpecifier { get; set; }
        public string ViewName { get; set; }
        public List<UriCondition> UriConditions { get; }
        public List<UriCondition> UriMetaConditions { get; }
        public IResource IResource => Resource.Find(ResourceSpecifier);
        public DbMacro Macro { get; set; }
        public Origin Origin { get; set; }
        public byte[] BodyBytes { get; set; }
        public IDictionary<string, string> Headers { get; set; }
        public string ContentType { get; set; }
        public string Accept { get; set; }
        public ResultFinalizer ResultFinalizer { get; set; }
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
                    writer.Write(UriConditions != null ? string.Join("&", UriConditions) : null);
                    writer.Write('/');
                    writer.Write(UriMetaConditions != null ? string.Join("&", UriMetaConditions) : null);
                    return writer.ToString().TrimEnd('/');
                }
            }
        }

        internal Arguments()
        {
            ResourceSpecifier = DefaultResourceSpecifier;
            UriConditions = new List<UriCondition>();
            UriMetaConditions = new List<UriCondition>();
        }
    }
}