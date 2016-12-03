using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using RESTar;
using Starcounter;

namespace RESTarExample
{
    /// <summary>
    /// By assigning the RESTar attribute and providing either the methods we want
    /// to enable, or a preset like ReadAndWrite (enables all methods), we register
    /// this class as a RESTar resource. This also makes it subject for serialization
    /// and deserialization to JSON objects, which we need to keep in mind when 
    /// designing our Starcounter data model and decide what should be exposed to RESTar.
    /// </summary>
    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class RESTarEmployee
    {
        public string Name;
        public string Title;

        /// <summary>
        /// 'Boss' is an object reference. When serializing an entity in the resource 
        /// RESTarPerson (a row in this table), RESTar will serialize the 'Boss' column
        /// as an inner JSON object. Example:
        /// {
        ///     "Name": "Tim Canterbury",
        ///     "Title": "Sales rep",
        ///     "Boss": {
        ///         "Name": "David Brent",
        ///         "Title": "Mentor, Entertainer, Renaissance man"
        ///         // David has no boss
        ///     } 
        /// }
        /// Often this is acceptable, but it can be problematic. If we want to ignore a 
        /// column (field or property) in a resource when serializing or deserializing a 
        /// JSON object, we decorate the corresponding member with the .NET standard 
        /// IgnoreDataMember attribute. In this case the reference is possibly recursive 
        /// (the boss may have a boss etc.) which makes it appropriate to ignore this column. 
        /// We can always include a new member holding the boss' ObjectNo though if we still 
        /// want to print useful information from the 'Boss' column. Substituting ObjectNo
        /// for object references in tables is how the Starcounter Administrator handles 
        /// this, so let's implement an similar pattern here where the 'Boss' column is
        /// decided by an ObjectNo, and not by an object reference.
        /// </summary>
        [IgnoreDataMember] // first we ignore this member
        public RESTarEmployee Boss // then we make it into an auto-property like so
        {
            get { return DbHelper.FromID(bossObjectNo) as RESTarEmployee; }
            set { bossObjectNo = value.GetObjectNo(); }
        }

        /// <summary>
        /// If we want to give the serializer another name than 'bossObjectNo' to use as 
        /// JSON member name, we can provide an alias using the .NET standard DataMember 
        /// attribute. This way we still have control over what the resulting JSON will 
        /// look like.
        /// </summary>
        [DataMember(Name = "Boss")]
        public ulong bossObjectNo;

        // This means that an entity of this resource will be serialized as (for example):
        // {
        //     "Name": "Dawn Tinsley",
        //     "Title": "Receptionist",
        //     "Boss": 39132
        // }
        // ... and that JSON object would deserialize properly to this resource since
        // the serializer will match "Boss" with the 'bossObjectNo' column, and write the
        // ulong to that column. The actual object is then referenced via a separate
        // property when accessed. Since the FromID method is lightning fast, this works
        // well. 

        /// <summary>
        /// Since the 'Boss' member is ignored, we can decide whether to include the 
        /// 'Underlings' column or not. Had we included both, the serializer would detect
        /// the infinite recursion and throw an exception when matching entities in this 
        /// resource. In this case, let's include 'Underlings'.
        /// </summary>
        public IEnumerable<RESTarEmployee> Underlings =>
            Db.SQL<RESTarEmployee>($"SELECT t FROM {GetType().FullName} t WHERE t.Boss =?", this);
    }

    // The above class is well-prepped to work as a RESTar resource. It is important to keep
    // the serialization in mind when registering database class definitions as RESTar resources.
    // Failing to mark possibly problematic internal object references with IgnoreDataMember 
    // can generate serious issues when serializing entities. Imagine if we include a list of 
    // other objects, which in turn include more objects etc. A simple GET command could then 
    // return thousands of JSON objects. There is no built-in protection against such commands 
    // (sometimes that is what you want to do), so a well designed data model is essential.
}