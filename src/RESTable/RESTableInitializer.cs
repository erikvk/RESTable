using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RESTable.Auth;
using RESTable.Internal;
using RESTable.Meta;
using RESTable.Meta.Internal;

namespace RESTable
{
    public class RESTableInitializer
    {
        public RESTableInitializer
        (
            TypeCache typeCache,
            ResourceCollection resourceCollection,
            ResourceFactory resourceFactory,
            ProtocolProviderManager protocolProviderManager,
            RootAccess rootAccess,
            IEnumerable<IStartupActivator> startupActivators,
            IApplicationServiceProvider applicationServiceProvider
        )
        {
            ApplicationServicesAccessor.ApplicationServiceProvider = applicationServiceProvider;
            resourceCollection.SetDependencies(typeCache, rootAccess);
            resourceFactory.MakeResources();
            rootAccess.Load();
            resourceFactory.BindControllers();
            protocolProviderManager.OnInit();
            resourceFactory.FinalCheck();
            var startupTasks = startupActivators.Select(activator => activator.Activate());
            Task.WhenAll(startupTasks).Wait();
        }
    }
}