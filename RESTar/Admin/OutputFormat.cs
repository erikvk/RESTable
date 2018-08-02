using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using RESTar.Linq;
using RESTar.Requests;
using RESTar.Resources;
using RESTar.Resources.Operations;
using Starcounter;

namespace RESTar.Admin
{
    /// <inheritdoc cref="ISelector{T}" />
    /// <inheritdoc cref="IInserter{T}" />
    /// <inheritdoc cref="IUpdater{T}" />
    /// <inheritdoc cref="IDeleter{T}" />
    /// <inheritdoc cref="IValidator{T}" />
    /// <summary>
    /// A resource for all available output formats for this RESTar instance.
    /// </summary>
    [RESTar(Description = description)]
    public class OutputFormat : ISelector<OutputFormat>, IInserter<OutputFormat>, IUpdater<OutputFormat>, IDeleter<OutputFormat>,
        IValidator<OutputFormat>
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
        public bool IsValid(OutputFormat entity, out string invalidReason)
        {
            if (string.IsNullOrWhiteSpace(entity.Name))
            {
                invalidReason = "Invalid or missing name";
                return false;
            }
            if (string.IsNullOrWhiteSpace(entity.Pattern))
            {
                invalidReason = "Invalid or missing pattern";
                return false;
            }
            if (!entity.Pattern.Contains("$data"))
                throw new Exception($"Invalid pattern string '{entity.Pattern}'. Must contain the '$data' macro.");
            if (entity.Pattern.IndexOf("$data", StringComparison.Ordinal) != entity.Pattern.LastIndexOf("$data", StringComparison.Ordinal))
                throw new Exception($"Invalid pattern string '{entity.Pattern}'. Can only contain one instance of the '$data' macro.");

            var (pre, post) = entity.Pattern.TSplit("$data");
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
            foreach (var entity in request.GetInputEntities())
            {
                if (DbOutputFormat.GetByName(entity.Name) != null)
                    throw new Exception($"Invalid name. '{entity.Name}' is already in use.");
                Db.TransactAsync(() => new DbOutputFormat {Name = entity.Name, RegularPattern = entity.Pattern});
                count += 1;
            }
            return count;
        }

        /// <inheritdoc />
        public int Update(IRequest<OutputFormat> request)
        {
            var count = 1;
            request.GetInputEntities().ForEach(entity =>
            {
                var dbEntity = DbOutputFormat.GetByName(entity.Name);
                if (dbEntity == null) return;
                Db.TransactAsync(() =>
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
            request.GetInputEntities().ForEach(entity =>
            {
                if (entity.IsBuiltIn) return;
                Db.TransactAsync(DbOutputFormat.GetByName(entity.Name).Delete);
                count += 1;
            });
            return count;
        }
    }
}