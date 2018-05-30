using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RESTar.Internal.Sc;
using RESTar.Linq;
using RESTar.Resources;
using RESTar.Resources.Operations;
using static RESTar.Internal.EntityResourceProviderController;

namespace RESTar.Meta.Internal
{
    internal static class ResourceFactory
    {
        private static DynamitResourceProvider DynamitProvider { get; }
        private static StarcounterDeclaredResourceProvider StarcounterDeclaredProvider { get; }
        private static VirtualResourceProvider VrProvider { get; }
        private static TerminalResourceProvider TerminalProvider { get; }
        private static BinaryResourceProvider BinaryProvider { get; }

        static ResourceFactory()
        {
            StarcounterDeclaredProvider = new StarcounterDeclaredResourceProvider();
            DynamitProvider = new DynamitResourceProvider(StarcounterDeclaredProvider.DatabaseIndexer);
            VrProvider = new VirtualResourceProvider();
            EntityResourceProviders.Add(DynamitProvider.GetProviderId(), DynamitProvider);
            EntityResourceProviders.Add(StarcounterDeclaredProvider.GetProviderId(), StarcounterDeclaredProvider);
            EntityResourceProviders.Add(VrProvider.GetProviderId(), VrProvider);
            TerminalProvider = new TerminalResourceProvider();
            BinaryProvider = new BinaryResourceProvider();
        }

        private static void ValidateResourceProviders(ICollection<EntityResourceProvider> externalProviders)
        {
            if (externalProviders == null) return;
            externalProviders.ForEach(p =>
            {
                if (p == null) throw new InvalidExternalResourceProviderException("Found null value in 'resourceProviders' array");
                p.Validate();
            });
            if (externalProviders.ContainsDuplicates(p => p.GetType().RESTarTypeName(), out var typeDupe))
                throw new InvalidExternalResourceProviderException(
                    $"Two or more external ResourceProviders with the same type '{typeDupe.GetType().RESTarTypeName()}' was found. Include " +
                    "only one in the call to RESTarConfig.Init()");
            if (externalProviders.Select(p => p.GetProviderId().ToLower()).ContainsDuplicates(out var idDupe))
                throw new InvalidExternalResourceProviderException(
                    "Two or more external ResourceProviders had simliar type names, which could lead to confusion. Only one provider " +
                    $"should be associated with '{idDupe}'");
            foreach (var provider in externalProviders)
                EntityResourceProviders.Add(provider.GetProviderId(), provider);
        }

        /// <summary>
        /// All types covered by RESTar are selected and validated here
        /// 
        /// Resources
        ///   entity
        ///     regular
        ///     wrapper
        ///   terminal
        ///   binary
        /// Views 
        /// 
        /// </summary>
        /// <returns></returns>
        private static void ValidateAndBuildTypeLists(out List<Type> regularTypes, out List<Type> wrapperTypes, out List<Type> terminalTypes,
            out List<Type> binaryTypes)
        {
            var allTypes = typeof(object).GetSubclasses().ToList();
            var resourceTypes = allTypes.Where(t => t.HasAttribute<RESTarAttribute>(out var a) && !(a is RESTarProceduralAttribute)).ToArray();
            var viewTypes = allTypes.Where(t => t.HasAttribute<RESTarViewAttribute>()).ToArray();
            if (resourceTypes.Union(viewTypes).ContainsDuplicates(t => t.RESTarTypeName()?.ToLower() ?? "unknown", out var dupe))
                throw new InvalidResourceDeclarationException("Types used by RESTar must have unique case insensitive names. Found " +
                                                              $"multiple types with case insensitive name '{dupe}'.");

            void ValidateViewTypes(IEnumerable<Type> types)
            {
                foreach (var type in types)
                {
                    var resource = type.DeclaringType;
                    if (!type.IsClass || !type.IsNestedPublic || resource == null)
                        throw new InvalidResourceViewDeclarationException(type,
                            "Resource view types must be declared as public classes nested within the the " +
                            "resource type they are views for");
                    if (type.IsSubclassOf(type))
                        throw new InvalidResourceViewDeclarationException(type, "Views cannot inherit from their resource types");

                    if (resource.Implements(typeof(IResourceWrapper)))
                    {
                        var wrapped = resource.GetWrappedType();
                        if (!type.Implements(typeof(ISelector<>), out var param) || param[0] != wrapped)
                            throw new InvalidResourceViewDeclarationException(type,
                                $"Expected view type to implement ISelector<{wrapped.RESTarTypeName()}>");
                    }
                    else if (!type.Implements(typeof(ISelector<>), out var param) || param[0] != resource)
                        throw new InvalidResourceViewDeclarationException(type,
                            $"Expected view type to implement ISelector<{resource.RESTarTypeName()}>");
                    var propertyUnion = resource.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Union(type.GetProperties(BindingFlags.Public | BindingFlags.Instance));
                    if (propertyUnion.ContainsDuplicates(p => p.RESTarMemberName(), StringComparer.OrdinalIgnoreCase, out var propDupe))
                        throw new InvalidResourceViewDeclarationException(type,
                            $"Invalid property '{propDupe.Name}'. Resource view types must not contain any public instance " +
                            "properties with the same name (case insensitive) as a property of the corresponding resource. " +
                            "All properties in the resource are automatically inherited for use in conditions with the view.");
                }
            }

            (regularTypes, wrapperTypes, terminalTypes, binaryTypes) = ResourceValidator.Validate(resourceTypes);
            ValidateViewTypes(viewTypes);
        }

        private static void ValidateInnerResources() => RESTarConfig.Resources
            .GroupBy(r => r.ParentResourceName)
            .Where(group => group.Key != null)
            .ForEach(group =>
            {
                var parentResource = (IResourceInternal) Resource.SafeGet(group.Key);
                if (parentResource == null)
                    throw new InvalidResourceDeclarationException(
                        $"Resource type(s) {string.Join(", ", group.Select(item => $"'{item.Name}'"))} is/are declared " +
                        $"within the scope of another class '{group.Key}', that is not a RESTar resource. Inner " +
                        "resources must be declared within a resource class.");
                parentResource.InnerResources = group.ToList();
            });

        internal static void MakeResources(EntityResourceProvider[] externalProviders)
        {
            ValidateResourceProviders(externalProviders);
            ValidateAndBuildTypeLists(out var regularTypes, out var wrapperTypes, out var terminalTypes, out var binaryTypes);

            foreach (var provider in EntityResourceProviders.Values)
            {
                var claim = provider.GetClaim(regularTypes);
                regularTypes = regularTypes.Except(claim).ToList();
                provider.MakeClaimRegular(claim);
            }

            foreach (var provider in EntityResourceProviders.Values)
            {
                var claim = provider.GetClaim(wrapperTypes);
                wrapperTypes = wrapperTypes.Except(claim).ToList();
                provider.MakeClaimWrapped(claim);
            }

            foreach (var provider in EntityResourceProviders.Values)
            {
                provider.ReceiveClaimed(Resource.ClaimedBy(provider));
                provider.MakeClaimProcedural();
            }

            TerminalProvider.RegisterTerminalTypes(terminalTypes);
            BinaryProvider.RegisterBinaryTypes(binaryTypes);
            ValidateInnerResources();
        }

        /// <summary>
        /// All resources are now in place and metadata can be built. This checks for any additional errors
        /// </summary>
        internal static void FinalCheck()
        {
            var metadata = Metadata.Get(MetadataLevel.Full);
            foreach (var (enumType, _) in metadata.PeripheralTypes.Where(t => t.Type.IsEnum))
            {
                if (Enum.GetNames(enumType).Select(name => name.ToLower()).ContainsDuplicates(out var dupe))
                    throw new InvalidReferencedEnumDeclarationException("A reference was made in some resource type to an enum type with name " +
                                                                        $"'{enumType.RESTarTypeName()}', containing multiple named constants equal to '{dupe}' " +
                                                                        "(case insensitive). All enum types referenced by some RESTar resource " +
                                                                        "type must have unique case insensitive named constants");
            }
        }
    }
}