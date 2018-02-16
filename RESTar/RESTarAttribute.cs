using System;
using System.Collections.Generic;
using System.Linq;
using static RESTar.Methods;

namespace RESTar
{
    /// <inheritdoc />
    /// <summary>
    /// Registers a new RESTar resource and provides permissions. If no methods are 
    /// provided in the constructor, all methods are made available for this resource.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class RESTarAttribute : Attribute
    {
        /// <summary>
        /// The methods declared as available for this RESTar resource. Not applicable for 
        /// terminal resources.
        /// </summary>
        public IReadOnlyList<Methods> AvailableMethods { get; }

        /// <summary>
        /// If true, unknown conditions encountered when handling incoming requests
        /// will be passed through as dynamic. This allows for a dynamic handling of
        /// members, both for condition matching and for entities returned from the 
        /// resource selector. Not applicable for terminal resources.
        /// </summary>
        public bool AllowDynamicConditions { get; set; }

        /// <summary>
        /// This will place a dollar sign ($) before all statically defined properties 
        /// for this type in the REST API, to avoid capture with dynamic members. Always 
        /// true for DDictionary resources. Not applicable for terminal resources.
        /// </summary>
        public bool FlagStaticMembers { get; set; }

        /// <summary>
        /// Should this resource be editable after registration? Not applicable for 
        /// terminal resources.
        /// </summary>
        internal bool Editable { get; set; }

        /// <summary>
        /// Singleton resources get special treatment in the view. They have no list 
        /// view, but only entity view. Good for settings, reports etc. Not applicable for 
        /// terminal resources.
        /// </summary>
        public bool Singleton { get; set; }

        /// <summary>
        /// Resource descriptions are visible in the AvailableResource resource
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Should this resource, with methods GET and REPORT, be included in all access scopes, 
        /// regardless of API keys in the configuration file? This is useful for global resources 
        /// in foreign assemblies. The API will still require API key if requireApiKey is set to 
        /// true in the call to RESTarConfig.Init(), but this resource will be included in each 
        /// key's scope.
        /// </summary>
        public bool GETAvailableToAll { get; set; }

        /// <inheritdoc />
        internal RESTarAttribute(IReadOnlyList<Methods> methods)
        {
            if (methods.Contains(GET))
                AvailableMethods = methods.Union(new[] {REPORT,HEAD}).ToList();
            else AvailableMethods = methods;
        }

        /// <inheritdoc />
        /// <summary>
        /// Registers a new RESTar resource and provides permissions. If no methods are 
        /// provided in the constructor, all methods are made available for this resource.
        /// </summary>
        public RESTarAttribute(params Methods[] methodRestrictions)
        {
            if (!methodRestrictions.Any())
                methodRestrictions = RESTarConfig.Methods;
            var restrictions = methodRestrictions.OrderBy(i => i, MethodComparer.Instance).ToList();
            if (methodRestrictions.Contains(GET) && !methodRestrictions.Contains(REPORT))
                restrictions.Add(REPORT);
            if (methodRestrictions.Contains(GET) && !methodRestrictions.Contains(HEAD))
                restrictions.Add(HEAD);
            AvailableMethods = restrictions;
        }
    }
}