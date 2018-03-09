using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using RESTar.Linq;
using RESTar.Operations;
using RESTar.Serialization;
using Starcounter;
using static RESTar.Admin.Settings;

namespace RESTar.Admin
{
    /// <summary>
    /// </summary>
    [Database]
    public class DbOutputFormat
    {
        #region Schema

        /// <summary>
        /// The name of the pattern
        /// </summary>
        public string Name { get; internal set; }

        private string _regularPattern;

        private const string placeholder = "\"__RESTar__\"";
        private const string macro = "$data";

        /// <summary>
        /// The serialization pattern when prettyprint is set to false
        /// </summary>
        public string RegularPattern
        {
            get => _regularPattern;
            internal set
            {
                _regularPattern = value;
                (RegularPre, RegularPost) = value.TSplit(macro);
                var prettyPrintPattern = Serializers.Json.SerializeFormatter(
                    JToken.Parse(RegularPre + placeholder + RegularPost), out var indents);
                (PrettyPrintPre, PrettyPrintPost) = prettyPrintPattern.TSplit(placeholder);
                StartIndent = indents;
            }
        }

        private bool _isDefault;

        /// <summary>
        /// Is this the default pattern?
        /// </summary>
        public bool IsDefault
        {
            get => _isDefault;
            set
            {
                if (value)
                {
                    GetAll().ForEach(f => f._isDefault = false);
                    _isDefault = true;
                }
                else _isDefault = false;
            }
        }

        /// <summary>
        /// Pre-data serialization pattern when prettyprint is set to false
        /// </summary>
        public string RegularPre { get; private set; }

        /// <summary>
        /// Post-data serialization pattern when prettyprint is set to false
        /// </summary>
        public string RegularPost { get; private set; }

        /// <summary>
        /// Pre-data serialization pattern when prettyprint is set to true
        /// </summary>
        public string PrettyPrintPre { get; private set; }

        /// <summary>
        /// Post-data serialization pattern when prettyprint is set to true
        /// </summary>
        public string PrettyPrintPost { get; private set; }

        /// <summary>
        /// The indentation level at which prettyprinted json is serialized
        /// </summary>
        public int StartIndent { get; private set; }

        #endregion

        internal DbOutputFormat() { }

        private const string RawPattern = macro;
        private const string SimplePattern = "{\"data\":$data}";
        private const string JSendPattern = "{\"status\":\"success\",\"data\":{\"posts\":$data}}";
        private const string All = "SELECT t FROM RESTar.Admin.DbOutputFormat t";
        private const string ByDefault = All + " WHERE t.IsDefault =?";
        private const string ByName = All + " WHERE t.Name =?";

        internal Formatter Format => _PrettyPrint
            ? new Formatter(PrettyPrintPre, PrettyPrintPost, StartIndent)
            : new Formatter(RegularPre, RegularPost, StartIndent);

        internal static Formatter Default => Db.SQL<DbOutputFormat>(ByDefault, true).FirstOrDefault()?.Format ?? default;
        internal static Formatter Raw => default;
        internal static IEnumerable<DbOutputFormat> GetAll() => Db.SQL<DbOutputFormat>(All);
        internal static DbOutputFormat GetByName(string formatName) => Db.SQL<DbOutputFormat>(ByName, formatName).FirstOrDefault();

        internal static void Init()
        {
            if (GetAll().All(format => format.Name != "Raw"))
                Transact.Trans(() => new DbOutputFormat {Name = "Raw", RegularPattern = RawPattern});
            if (GetAll().All(format => format.Name != "Simple"))
                Transact.Trans(() => new DbOutputFormat {Name = "Simple", RegularPattern = SimplePattern});
            if (GetAll().All(format => format.Name != "JSend"))
                Transact.Trans(() => new DbOutputFormat {Name = "JSend", RegularPattern = JSendPattern});
            if (GetAll().All(format => !format.IsDefault))
            {
                var raw = Db.SQL<DbOutputFormat>(ByName, "Raw").First();
                Transact.Trans(() => raw._isDefault = true);
            }
        }
    }

    /// <inheritdoc cref="ISelector{T}" />
    /// <inheritdoc cref="IInserter{T}" />
    /// <inheritdoc cref="IUpdater{T}" />
    /// <inheritdoc cref="IDeleter{T}" />
    /// <inheritdoc cref="IValidatable" />
    /// <summary>
    /// A resource for all available output formats for this RESTar instance.
    /// </summary>
    [RESTar(Description = description)]
    public class OutputFormat : ISelector<OutputFormat>, IInserter<OutputFormat>, IUpdater<OutputFormat>, IDeleter<OutputFormat>,
        IValidatable
    {
        private const string description = "Contains all available output formats for this RESTar instance";

        private static readonly JArray ExampleArray = new JArray
        {
            new JObject {["Property1"] = "Value1", ["Property2"] = "Value2"},
            new JObject {["Property1"] = "Value3", ["Property2"] = "Value4"}
        };

        /// <summary>
        /// The name of the output format
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The pattern of the output format
        /// </summary>
        public string Pattern { get; set; }

        /// <summary>
        /// Is this the default pattern?
        /// </summary>
        public bool IsDefault { get; set; }

        /// <summary>
        /// Example object
        /// </summary>
        public JToken Example { get; private set; }

        /// <inheritdoc />
        public bool IsValid(out string invalidReason)
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                invalidReason = "Invalid or missing name";
                return false;
            }
            if (string.IsNullOrWhiteSpace(Pattern))
            {
                invalidReason = "Invalid or missing pattern";
                return false;
            }
            if (!Pattern.Contains("$data"))
                throw new Exception($"Invalid pattern string '{Pattern}'. Must contain the '$data' macro.");
            if (Pattern.IndexOf("$data", StringComparison.Ordinal) != Pattern.LastIndexOf("$data", StringComparison.Ordinal))
                throw new Exception($"Invalid pattern string '{Pattern}'. Can only contain one instance of the '$data' macro.");

            var (pre, post) = Pattern.TSplit("$data");
            try
            {
                JToken.Parse(pre + "[]" + post);
            }
            catch
            {
                invalidReason = "Invalid pattern. Check JSON syntax.";
                return false;
            }

            invalidReason = null;
            return true;
        }

        internal bool IsBuiltIn => Name == "Raw" || Name == "Simple" || Name == "JSend";

        /// <inheritdoc />
        public IEnumerable<OutputFormat> Select(IRequest<OutputFormat> request)
        {
            DbOutputFormat.Init();
            return DbOutputFormat.GetAll()
                .Select(f => new OutputFormat
                {
                    Name = f.Name,
                    Pattern = f.RegularPattern,
                    IsDefault = f.IsDefault,
                    Example = JToken.Parse(f.RegularPattern.Replace("$data", ExampleArray.ToString()))
                })
                .Where(request.Conditions);
        }

        /// <inheritdoc />
        public int Insert(IRequest<OutputFormat> request)
        {
            var count = 0;
            foreach (var entity in request.GetEntities())
            {
                if (DbOutputFormat.GetByName(entity.Name) != null)
                    throw new Exception($"Invalid name. '{entity.Name}' is already in use.");
                Transact.Trans(() => new DbOutputFormat {Name = entity.Name, RegularPattern = entity.Pattern});
                count += 1;
            }
            return count;
        }

        /// <inheritdoc />
        public int Update(IRequest<OutputFormat> request)
        {
            var count = 1;
            request.GetEntities().ForEach(entity =>
            {
                var dbEntity = DbOutputFormat.GetByName(entity.Name);
                if (dbEntity == null) return;
                Transact.Trans(() =>
                {
                    count += 1;
                    dbEntity.IsDefault = entity.IsDefault;
                    if (entity.IsBuiltIn) return;
                    dbEntity.RegularPattern = entity.Pattern;
                });
            });
            DbOutputFormat.Init();
            return count;
        }

        /// <inheritdoc />
        public int Delete(IRequest<OutputFormat> request)
        {
            var count = 0;
            request.GetEntities().ForEach(entity =>
            {
                if (entity.IsBuiltIn) return;
                Transact.Trans(DbOutputFormat.GetByName(entity.Name).Delete);
                count += 1;
            });
            return count;
        }
    }
}