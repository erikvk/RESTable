﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RESTar.Linq;
using System.Runtime.Serialization;
using RESTar.Internal;
using static System.Reflection.BindingFlags;

namespace RESTar.Resources
{
    internal static class ResourceFactory
    {
        internal static DDictionaryProvider DDictProvider { get; }
        internal static StarcounterProvider ScProvider { get; }
        internal static VirtualResourceProvider VrProvider { get; }
        internal static DynamicResourceProvider DynProvider { get; }
        private static List<ResourceProvider> ResourceProviders { get; }

        static ResourceFactory()
        {
            DDictProvider = new DDictionaryProvider();
            ScProvider = new StarcounterProvider();
            VrProvider = new VirtualResourceProvider();
            DynProvider = new DynamicResourceProvider();
            ResourceProviders = new List<ResourceProvider> {DDictProvider, ScProvider, VrProvider};
        }

        private static void ValidateCommon(Type type)
        {
            #region Check general stuff

            if (type.FullName == null)
                throw new ResourceDeclarationException("Encountered an unknown type. No further information is available.");

            if (type.FullName.Count(c => c == '+') >= 2)
                throw new ResourceDeclarationException($"Invalid resource '{type.FullName.Replace('+', '.')}'. " +
                                                       "Inner resources cannot have their own inner resources");

            if (type.FullName.ToLower().StartsWith("restar.") && type.Assembly != typeof(Resource).Assembly)
                throw new ResourceDeclarationException(
                    $"Invalid namespace for resource type '{type.FullName}'. Cannot begin with \'RESTar\' " +
                    "or any case variants of \'RESTar\'");

            if ((!type.IsClass || !type.IsPublic && !type.IsNestedPublic) && type.Assembly != typeof(Resource).Assembly)
                throw new ResourceDeclarationException($"Invalid type '{type.FullName}'. Resource types must be public classes");

            #endregion

            #region Check for invalid IDictionary implementation

            if (type.Implements(typeof(IDictionary<,>), out var typeParams) && typeParams[0] != typeof(string))
                throw new ResourceDeclarationException(
                    $"Invalid resource declaration for type '{type.FullName}'. All resources implementing " +
                    "the generic 'System.Collections.Generic.IDictionary`2' interface must have System.String as " +
                    $"first type parameter. Found {typeParams[0].FullName}");

            #endregion

            #region Check for invalid IEnumerable implementation

            if ((type.Implements(typeof(IEnumerable<>)) || type.Implements(typeof(IEnumerable))) &&
                !type.Implements(typeof(IDictionary<,>)))
                throw new ResourceDeclarationException(
                    $"Invalid resource declaration for type '{type.FullName}'. The type has an invalid imple" +
                    $"mentation of an IEnumerable interface. The resource '{type.FullName}' (or any of its base types) " +
                    "cannot implement the \'System.Collections.Generic.IEnumerable`1\' or \'System.Collections.IEnume" +
                    "rable\' interfaces without also implementing the \'System.Collections.Generic.IDictionary`2\' interface."
                );

            #endregion

            #region Check for public instance fields

            var fields = type.GetFields(Public | Instance);
            if (fields.Any())
                throw new ResourceMemberException(
                    "A virtual resource cannot include public instance fields, " +
                    $"only properties. Resource: '{type.FullName}' Fields: {string.Join(", ", fields.Select(f => $"'{f.Name}'"))} in resource '{type.FullName}'"
                );

            #endregion

            #region Check for properties with duplicate case insensitive names

            if (type.GetProperties(Public | Instance)
                .Where(p => !p.HasAttribute<IgnoreDataMemberAttribute>())
                .Where(p => !(p.DeclaringType.Implements(typeof(IDictionary<,>)) && p.Name == "Item"))
                .Select(p => p.RESTarMemberName().ToLower())
                .ContainsDuplicates(out var duplicate))
                throw new ResourceMemberException(
                    $"Invalid properties for resource '{type.FullName}'. Names of public instance properties declared " +
                    $"for a virtual resource must be unique (case insensitive). Two or more property names evaluated to {duplicate}."
                );

            #endregion        }
        }

        private static void ValidateWrapperDeclaration(Type type)
        {
            ValidateCommon(type);
            var wrappedType = type.GetGenericArguments()[0];
            if (wrappedType.HasAttribute<RESTarAttribute>())
                throw new ResourceWrapperException("RESTar found a RESTar.ResourceWrapper declaration for type " +
                                                   $"'{wrappedType.FullName}', a type that is already a RESTar " +
                                                   $"resource type. Only non-resource types can be wrapped.");
            if (wrappedType.FullName?.StartsWith("RESTar") == true)
                throw new ResourceWrapperException("RESTar found an invalid RESTar.ResourceWrapper declaration for type " +
                                                   $"'{wrappedType.FullName}'. RESTar types cannot be wrapped.");
        }

        private static void ValidateResourceDeclaration(Type type)
        {
            ValidateCommon(type);
        }

        private static void ValidateInnerResources() => RESTarConfig.Resources
            .GroupBy(r => r.ParentResourceName)
            .Where(group => group.Key != null)
            .ForEach(group =>
            {
                var parentResource = (IResourceInternal) Resource.SafeGet(group.Key);
                if (parentResource == null)
                    throw new ResourceDeclarationException(
                        $"Resource types {string.Join(", ", group.Select(item => $"'{item.Name}'"))} are declared " +
                        $"within the scope of another class '{group.Key}', that is not a RESTar resource. Inner " +
                        "resources must be declared within a resource class.");
                parentResource.InnerResources = group.ToList();
            });

        internal static void MakeResources(ResourceProvider[] externalProviders)
        {
            if (externalProviders != null)
            {
                externalProviders.ForEach(e => e.Validate());
                if (externalProviders.ContainsDuplicates(p => p.GetType().FullName, out var dupe))
                    throw new ExternalResourceProviderException("Two or more external ResourceProviders with the same " +
                                                                $"type '{dupe}' was found. Include only one in the call " +
                                                                "to RESTarConfig.Init()");
                ResourceProviders.AddRange(externalProviders);
            }

            var regularResourceTypes = typeof(object).GetSubclasses()
                .Where(t => t.HasAttribute<RESTarAttribute>())
                .Where(t => !typeof(IResourceWrapper).IsAssignableFrom(t))
                .ToList();
            regularResourceTypes.ForEach(ValidateResourceDeclaration);

            var wrapperTypes = typeof(object).GetSubclasses()
                .Where(t => t.HasAttribute<RESTarAttribute>())
                .Where(t => typeof(IResourceWrapper).IsAssignableFrom(t))
                .ToList();
            wrapperTypes.ForEach(ValidateWrapperDeclaration);

            foreach (var provider in ResourceProviders)
            {
                var claim = provider.GetClaim(regularResourceTypes);
                regularResourceTypes = regularResourceTypes.Except(claim).ToList();
                provider.MakeClaimRegular(claim);
            }

            foreach (var provider in ResourceProviders)
            {
                var claim = provider.GetClaim(wrapperTypes);
                provider.MakeClaimWrapped(claim);
            }

            foreach (var provider in ResourceProviders)
                provider.ReceiveClaimed(Resource.ClaimedBy(provider));

            DynamicResource.All.ForEach(MakeDynamicResource);
        }

        internal static void MakeDynamicResource(DynamicResource resource) => DynProvider.BuildDynamicResource(resource);
    }
}