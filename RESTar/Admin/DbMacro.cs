using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using RESTar.Requests;
using Starcounter;

namespace RESTar.Admin
{
    /// <summary>
    /// The underlying storage for macros
    /// </summary>
    [Database, Obsolete]
    public class DbMacro
    {
        internal const string All = "SELECT t FROM RESTar.Admin.DbMacro t";
        internal const string ByName = All + " WHERE t.Name =?";

        #region Schema

        /// <summary>
        /// The name of the macro
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The resource locator to use in requests
        /// </summary>
        public string ResourceSpecifier { get; set; }

        /// <summary>
        /// The view, if any, to use in requests
        /// </summary>
        public string ViewName { get; set; }

        /// <summary>
        /// The uri conditions to append to requests
        /// </summary>
        public string UriConditionsString { get; set; }

        /// <summary>
        /// The uri meta-conditions to append to requests
        /// </summary>
        public string UriMetaConditionsString { get; set; }

        /// <summary>
        /// The URI of the macro
        /// </summary>
        public string UriString
        {
            get
            {
                var uriConditions = UriConditions;
                var uriMetaConditions = UriMetaConditions;
                using (var writer = new StringWriter())
                {
                    writer.Write('/');
                    writer.Write(ResourceSpecifier);
                    writer.Write('/');
                    writer.Write(uriConditions != null ? string.Join("&", uriConditions) : null);
                    writer.Write('/');
                    writer.Write(uriMetaConditions != null ? string.Join("&", uriMetaConditions) : null);
                    return writer.ToString().TrimEnd('/');
                }
            }
        }

        /// <summary>
        /// The body of the macro
        /// </summary>
        public Binary BodyBinary { get; set; }

        internal string BodyUTF8 => !HasBody ? "" : Encoding.UTF8.GetString(BodyBinary.ToArray());

        /// <summary>
        /// The headers of the macro
        /// </summary>
        public string Headers { get; set; }

        /// <summary>
        /// Should the macro overwrite matching headers in the calling request?
        /// </summary>
        public bool OverwriteHeaders { get; set; }

        /// <summary>
        /// Should the macro overwrite the body of the calling request?
        /// </summary>
        public bool OverwriteBody { get; set; }

        /// <summary />
        [Obsolete] public string Uri { get; set; }

        /// <summary>
        /// A dictionary representation of the headers for this macro
        /// </summary>
        internal Headers HeadersDictionary
        {
            get
            {
                if (Headers == null) return null;
                return JsonConvert.DeserializeObject<Headers>(Headers);
            }
        }

        #endregion

        internal UriCondition[] UriConditions => UriConditionsString?.Split('&').Select(c => new UriCondition(c)).ToArray();
        internal UriCondition[] UriMetaConditions => UriMetaConditionsString?.Split('&').Select(c => new UriCondition(c)).ToArray();
        internal bool HasBody => !BodyBinary.IsNull && BodyBinary.Length > 0;
        internal byte[] GetBody() => HasBody ? BodyBinary.ToArray() : new byte[0];

        internal static IEnumerable<DbMacro> GetAll() => Db.SQL<DbMacro>(All);
        internal static DbMacro Get(string macroName) => Db.SQL<DbMacro>(ByName, macroName).FirstOrDefault();
    }
}