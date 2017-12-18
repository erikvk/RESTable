using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using RESTar.Linq;
using RESTar.Operations;
using RESTar.Requests;
using Starcounter;

namespace RESTar.Admin
{
    /// <summary>
    /// The underlying storage for macros
    /// </summary>
    [Database]
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
        /// The URI of the macro
        /// </summary>
        public string Uri { get; set; }

        /// <summary>
        /// The body of the macro
        /// </summary>
        public Binary BodyBinary { get; set; }

        internal string BodyUTF8 => Encoding.UTF8.GetString(BodyBinary.ToArray());

        /// <summary>
        /// The headers of the macro
        /// </summary>
        public string Headers { get; set; }

        #endregion

        internal static IEnumerable<DbMacro> GetAll() => Db.SQL<DbMacro>(All);
        internal static DbMacro Get(string macroName) => Db.SQL<DbMacro>(ByName, macroName).FirstOrDefault();
    }

    /// <summary>
    /// A resource for all macros available for this RESTar instance
    /// </summary>
    [RESTar(Description = description)]
    public class Macro : ISelector<Macro>, IInserter<Macro>, IUpdater<Macro>, IDeleter<Macro>, IValidatable
    {
        private const string description = "Contains all available macros for this RESTar instance";

        /// <summary>
        /// The name of the macro
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The URI of the macro
        /// </summary>
        public string Uri { get; set; }

        /// <summary>
        /// The body of the macro
        /// </summary>
        public JToken Body { get; set; }

        /// <summary>
        /// The headers of the macro
        /// </summary>
        public JObject Headers { get; set; }

        /// <inheritdoc />
        /// <summary>
        /// Validates the macro
        /// </summary>
        public bool IsValid(out string invalidReason)
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                invalidReason = "Invalid or missing name";
                return false;
            }
            if (string.IsNullOrWhiteSpace(Uri))
            {
                invalidReason = "Invalid or missing URI";
                return false;
            }
            if (Uri.ToLower().Contains($"${Name.ToLower()}"))
            {
                invalidReason = "Macro URIs cannot contain self-references";
                return false;
            }
            try
            {
                var args = new RequestArguments(Uri);
                if (args.HasMetaConditions && args.UriMetaConditions.ToLower().Contains("key="))
                {
                    invalidReason = "Macro URIs cannot contain the 'Key' meta-condition. If API keys are " +
                                    "required, they are expected in each call to the macro.";
                    return false;
                }
            }
            catch
            {
                invalidReason = $"Invalid format for URI '{Uri}'.";
                return false;
            }
            if (Headers != null)
            {
                foreach (var prop in Headers)
                {
                    if (prop.Value.Type != JTokenType.String)
                    {
                        invalidReason = $"Invalid value for header '{prop.Key}'. Value must be string";
                        return false;
                    }
                    if (prop.Key.ToLower() == "authorization")
                    {
                        invalidReason = "Macro headers cannot contain the Authorization header. If API keys are " +
                                        "required, they are expected in each call to the macro.";
                        return false;
                    }
                }
            }
            invalidReason = null;
            return true;
        }

        /// <inheritdoc />
        public IEnumerable<Macro> Select(IRequest<Macro> request) => DbMacro.GetAll()
            .Select(m => new Macro
            {
                Name = m.Name,
                Uri = m.Uri,
                Body = m.BodyBinary != default ? JToken.Parse(m.BodyUTF8) : null,
                Headers = m.Headers != null ? JObject.Parse(m.Headers) : null
            })
            .Where(request.Conditions);

        /// <inheritdoc />
        public int Insert(IEnumerable<Macro> entities, IRequest<Macro> request)
        {
            var count = 0;
            foreach (var entity in entities)
            {
                if (DbMacro.Get(entity.Name) != null)
                    throw new Exception($"Invalid name. '{entity.Name}' is already in use.");
                Transact.Trans(() => new DbMacro
                {
                    Name = entity.Name,
                    Uri = entity.Uri,
                    BodyBinary = entity.Body != null ? new Binary(Encoding.UTF8.GetBytes(entity.Body?.ToString())) : default,
                    Headers = entity.Headers?.ToString()
                });
                count += 1;
            }
            return count;
        }

        /// <inheritdoc />
        public int Update(IEnumerable<Macro> entities, IRequest<Macro> request)
        {
            var count = 0;
            entities.ForEach(entity =>
            {
                var dbEntity = DbMacro.Get(entity.Name);
                if (dbEntity == null) return;
                Transact.Trans(() =>
                {
                    dbEntity.Uri = entity.Uri;
                    dbEntity.BodyBinary =
                        entity.Body != null ? new Binary(Encoding.UTF8.GetBytes(entity.Body?.ToString())) : default;
                    dbEntity.Headers = entity.Headers?.ToString();
                    count += 1;
                });
            });
            return count;
        }

        /// <inheritdoc />
        public int Delete(IEnumerable<Macro> entities, IRequest<Macro> request)
        {
            var count = 0;
            entities.ForEach(entity =>
            {
                Transact.Trans(DbMacro.Get(entity.Name).Delete);
                count += 1;
            });
            return count;
        }
    }
}