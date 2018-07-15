using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RESTar.Linq;
using RESTar.Requests;
using RESTar.Resources;
using RESTar.Resources.Operations;
using Starcounter;

namespace RESTar.Admin {
    /// <inheritdoc cref="ISelector{T}" />
    /// <inheritdoc cref="IInserter{T}" />
    /// <inheritdoc cref="IUpdater{T}" />
    /// <inheritdoc cref="IDeleter{T}" />
    /// <inheritdoc cref="IValidatable" />
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
        [RESTarMember(replaceOnUpdate: true)] public JToken Body { get; set; }

        /// <summary>
        /// The headers of the macro
        /// </summary>
        [RESTarMember(replaceOnUpdate: true)] public Headers Headers { get; set; }

        /// <summary>
        /// Should the macro overwrite the body of the calling request?
        /// </summary>
        public bool OverwriteBody { get; set; }

        /// <summary>
        /// Should the macro overwrite matching headers in the calling request?
        /// </summary>
        public bool OverwriteHeaders { get; set; }

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
                if (URI.Parse(Uri).MetaConditions.Any(c => ExtensionMethods.EqualsNoCase(c.Key, "key")))
                {
                    invalidReason = "Macro URIs cannot contain the 'Key' meta-condition. If API keys are " +
                                    "required, they are expected in each call to the macro.";
                    return false;
                }
            }
            catch (Exception e)
            {
                invalidReason = $"Invalid format for URI '{Uri}'. " + e.Message;
                return false;
            }

            if (Headers != null)
            {
                foreach (var prop in Headers)
                {
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
                Uri = m.UriString,
                Body = m.HasBody ? JToken.Parse(m.BodyUTF8) : null,
                Headers = m.Headers != null ? m.HeadersDictionary : null,
                OverwriteBody = m.OverwriteBody,
                OverwriteHeaders = m.OverwriteHeaders
            })
            .Where(request.Conditions);

        /// <inheritdoc />
        public int Insert(IRequest<Macro> request)
        {
            var count = 0;
            foreach (var entity in request.GetInputEntities())
            {
                if (DbMacro.Get(entity.Name) != null)
                    throw new Exception($"Invalid name. '{entity.Name}' is already in use.");
                var args = URI.Parse(entity.Uri);
                Db.TransactAsync(() => new DbMacro
                {
                    Name = entity.Name,
                    ResourceSpecifier = args.ResourceSpecifier,
                    ViewName = args.ViewName,
                    OverwriteBody = entity.OverwriteBody,
                    OverwriteHeaders = entity.OverwriteHeaders,
                    UriConditionsString = args.Conditions.Any() ? string.Join("&", args.Conditions) : null,
                    UriMetaConditionsString = args.MetaConditions.Any() ? string.Join("&", args.MetaConditions) : null,
                    BodyBinary = entity.Body != null ? new Binary(Encoding.UTF8.GetBytes(entity.Body?.ToString())) : default,
                    Headers = entity.Headers != null ? JsonConvert.SerializeObject(entity.Headers) : null
                });
                count += 1;
            }

            return count;
        }

        /// <inheritdoc />
        public int Update(IRequest<Macro> request)
        {
            var count = 0;
            request.GetInputEntities().ForEach(entity =>
            {
                var dbEntity = DbMacro.Get(entity.Name);
                if (dbEntity == null) return;
                var args = URI.Parse(entity.Uri);
                Db.TransactAsync(() =>
                {
                    dbEntity.ResourceSpecifier = args.ResourceSpecifier;
                    dbEntity.ViewName = args.ViewName;
                    dbEntity.OverwriteBody = entity.OverwriteBody;
                    dbEntity.OverwriteHeaders = entity.OverwriteHeaders;
                    dbEntity.UriConditionsString = args.Conditions.Any() ? string.Join("&", args.Conditions) : null;
                    dbEntity.UriMetaConditionsString = args.MetaConditions.Any() ? string.Join("&", args.MetaConditions) : null;
                    dbEntity.BodyBinary = entity.Body != null ? new Binary(Encoding.UTF8.GetBytes(entity.Body?.ToString())) : default;
                    dbEntity.Headers = entity.Headers != null ? JsonConvert.SerializeObject(entity.Headers) : null;
                    count += 1;
                });
            });
            return count;
        }

        /// <inheritdoc />
        public int Delete(IRequest<Macro> request)
        {
            var count = 0;
            request.GetInputEntities().ForEach(entity =>
            {
                Db.TransactAsync(DbMacro.Get(entity.Name).Delete);
                count += 1;
            });
            return count;
        }
    }
}