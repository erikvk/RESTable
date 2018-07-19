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
        public string Name { get; }

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
            Name = name;
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
            if (MakeUriComponents(out var error) == null)
            {
                invalidReason = error.LogMessage;
                return IsValid = false;
            }
            if (Headers.Authorization != null)
            {
                invalidReason = "Macro headers cannot contain the 'Authorization' header. If API keys are " +
                                "required, they are expected in each request invoking the macro.";
                return IsValid = false;
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
    }
}