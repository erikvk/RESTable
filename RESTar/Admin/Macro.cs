using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RESTar.ContentTypeProviders;
using RESTar.Internal;
using RESTar.ProtocolProviders;
using RESTar.Requests;
using RESTar.Resources;
using RESTar.Resources.Operations;
using Starcounter;
using static System.StringComparison;

namespace RESTar.Admin
{
    /// <inheritdoc cref="IValidatable" />
    /// <summary>
    /// A resource for all macros available for this RESTar instance
    /// </summary>
    [Database, RESTar(Description = description)]
    public class Macro : IMacro, IValidatable, IUriComponents
    {
        private const string description = "Contains all available macros for this RESTar instance";
        internal const string All = "SELECT t FROM RESTar.Admin.Macro t";
        internal const string ByName = All + " WHERE t.Name =?";

        /// <inheritdoc />
        public string Name { get; set; }

        /// <summary>
        /// The URI of the macro
        /// </summary>
        public string Uri { get; set; }

        /// <summary>
        /// The body of the macro
        /// </summary>
        [RESTarMember(replaceOnUpdate: true)] public JToken Body
        {
            get => HasBody ? JToken.Parse(Encoding.UTF8.GetString(BodyBinary.ToArray())) : null;
            set => BodyBinary = value != null ? new Binary(Encoding.UTF8.GetBytes(Providers.Json.Serialize(value, Formatting.None))) : default;
        }

        /// <summary>
        /// The headers of the macro
        /// </summary>
        public DbHeaders Headers { get; }

        /// <inheritdoc />
        public bool OverwriteBody { get; set; }

        /// <inheritdoc />
        public bool OverwriteHeaders { get; set; }

        /// <summary>
        /// Is this macro currently valid?
        /// </summary>
        public bool IsValid { get; private set; }

        /// <summary>
        /// The underlying storage for Body
        /// </summary>
        [RESTarMember(ignore: true)] public Binary BodyBinary { get; set; }

        /// <inheritdoc />
        [RESTarMember(ignore: true)] public bool HasBody => !BodyBinary.IsNull && BodyBinary.Length > 0;

        /// <inheritdoc />
        [JsonConstructor]
        public Macro(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Missing or invalid macro name");
            if (!Db.SQL<Macro>(ByName, name).All(macro => macro.Equals(this)))
                throw new ArgumentException($"Invalid macro name. '{name}' is already used by another macro");
            Headers = new DbHeaders();
        }

        /// <inheritdoc />
        /// <summary>
        /// Validates the macro
        /// </summary>
        bool IValidatable.IsValid(out string invalidReason)
        {
            if (string.IsNullOrWhiteSpace(Uri))
            {
                invalidReason = "Invalid or missing URI";
                return IsValid = false;
            }
            if (Uri.IndexOf($"${Name}", OrdinalIgnoreCase) >= 0)
            {
                invalidReason = "Macro URIs cannot contain self-references";
                return IsValid = false;
            }
            if (MakeUriComponents(out var error) is IUriComponents components && error != null) { }

            try
            {
                if (URI.Parse(Uri).MetaConditions.Any(c => c.Key.EqualsNoCase("key")))
                {
                    invalidReason = "Macro URIs cannot contain the 'Key' meta-condition. If API keys are " +
                                    "required, they are expected in each call to the macro.";
                    return IsValid = false;
                }
            }
            catch (Exception e)
            {
                invalidReason = $"Invalid format for URI '{Uri}'. " + e.Message;
                return IsValid = false;
            }

            if (Headers != null)
            {
                foreach (var prop in Headers)
                {
                    if (prop.Key.ToLower() == "authorization")
                    {
                        invalidReason = "Macro headers cannot contain the Authorization header. If API keys are " +
                                        "required, they are expected in each call to the macro.";
                        return IsValid = false;
                    }
                }
            }

            invalidReason = null;
            return IsValid = true;
        }

        #region IUriComponents

        [Transient] private IUriComponents uriComponents;

        private IUriComponents MakeUriComponents(out Results.Error error)
        {
            if (Context.Root.UriIsValid(Uri, out error, out _, out var components))
            {
                Uri = components.ToUriString();
                return components;
            }
            return null;
        }

        /// <summary>
        /// If asked for through IUriComponents API, require IsValid for non null value
        /// </summary>
        private IUriComponents UriComponents => IsValid ? uriComponents ?? (uriComponents = MakeUriComponents(out _)) : null;

        string IUriComponents.ToUriString() => UriComponents?.ToUriString();
        IProtocolProvider IUriComponents.ProtocolProvider => ProtocolController.DefaultProtocolProvider.ProtocolProvider;
        string IUriComponents.ResourceSpecifier => UriComponents?.ResourceSpecifier;
        string IUriComponents.ViewName => UriComponents?.ViewName;
        IReadOnlyCollection<IUriCondition> IUriComponents.Conditions => UriComponents?.Conditions;
        IReadOnlyCollection<IUriCondition> IUriComponents.MetaConditions => UriComponents?.MetaConditions;

        #endregion

        #region IMacro

        IMacro IUriComponents.Macro => this;
        byte[] IMacro.Body => HasBody ? BodyBinary.ToArray() : new byte[0];
        ContentType IMacro.ContentType => ContentType.JSON;
        IHeaders IMacro.Headers => Headers;

        #endregion

        ///// <inheritdoc />
        //public IEnumerable<Macro> Select(IRequest<Macro> request) => DbMacro.GetAll()
        //    .Select(m => new Macro
        //    {
        //        Name = m.Name,
        //        Uri = m.UriString,
        //        Body = m.HasBody ? JToken.Parse(m.BodyUTF8) : null,
        //        Headers = m.Headers != null ? m.HeadersDictionary : null,
        //        OverwriteBody = m.OverwriteBody,
        //        OverwriteHeaders = m.OverwriteHeaders
        //    })
        //    .Where(request.Conditions);

        ///// <inheritdoc />
        //public int Insert(IRequest<Macro> request)
        //{
        //    var count = 0;
        //    foreach (var entity in request.GetInputEntities())
        //    {
        //        if (DbMacro.Get(entity.Name) != null)
        //            throw new Exception($"Invalid name. '{entity.Name}' is already in use.");
        //        var args = URI.Parse(entity.Uri);
        //        Db.TransactAsync(() => new DbMacro
        //        {
        //            Name = entity.Name,
        //            ResourceSpecifier = args.ResourceSpecifier,
        //            ViewName = args.ViewName,
        //            OverwriteBody = entity.OverwriteBody,
        //            OverwriteHeaders = entity.OverwriteHeaders,
        //            UriConditionsString = args.Conditions.Any() ? string.Join("&", args.Conditions) : null,
        //            UriMetaConditionsString = args.MetaConditions.Any() ? string.Join("&", args.MetaConditions) : null,
        //            BodyBinary = entity.Body != null ? new Binary(Encoding.UTF8.GetBytes(entity.Body?.ToString())) : default,
        //            Headers = entity.Headers != null ? JsonConvert.SerializeObject(entity.Headers) : null
        //        });
        //        count += 1;
        //    }

        //    return count;
        //}

        ///// <inheritdoc />
        //public int Update(IRequest<Macro> request)
        //{
        //    var count = 0;
        //    request.GetInputEntities().ForEach(entity =>
        //    {
        //        var dbEntity = DbMacro.Get(entity.Name);
        //        if (dbEntity == null) return;
        //        var args = URI.Parse(entity.Uri);
        //        Db.TransactAsync(() =>
        //        {
        //            dbEntity.ResourceSpecifier = args.ResourceSpecifier;
        //            dbEntity.ViewName = args.ViewName;
        //            dbEntity.OverwriteBody = entity.OverwriteBody;
        //            dbEntity.OverwriteHeaders = entity.OverwriteHeaders;
        //            dbEntity.UriConditionsString = args.Conditions.Any() ? string.Join("&", args.Conditions) : null;
        //            dbEntity.UriMetaConditionsString = args.MetaConditions.Any() ? string.Join("&", args.MetaConditions) : null;
        //            dbEntity.BodyBinary = entity.Body != null ? new Binary(Encoding.UTF8.GetBytes(entity.Body?.ToString())) : default;
        //            dbEntity.Headers = entity.Headers != null ? JsonConvert.SerializeObject(entity.Headers) : null;
        //            count += 1;
        //        });
        //    });
        //    return count;
        //}

        ///// <inheritdoc />
        //public int Delete(IRequest<Macro> request)
        //{
        //    var count = 0;
        //    request.GetInputEntities().ForEach(entity =>
        //    {
        //        Db.TransactAsync(DbMacro.Get(entity.Name).Delete);
        //        count += 1;
        //    });
        //    return count;
        //}
    }
}