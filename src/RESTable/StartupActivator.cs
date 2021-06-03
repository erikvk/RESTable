using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace RESTable
{
    /// <summary>
    /// Used to register service types that should be loaded immediately on startup, to
    /// not delay error reporting to when they are activated.
    /// </summary>
    public interface IStartupActivator
    {
        Task Activate();
    }

    /// <summary>
    /// Used to register service types that should be loaded immediately on startup, to
    /// not delay error reporting to when they are activated.
    /// </summary>
    public class StartupActivator<TService> : IStartupActivator where TService : class
    {
        private IServiceProvider ServiceProvider { get; }
        private Func<TService, Task> OnActivate { get; }

        public StartupActivator(IServiceProvider serviceProvider, Func<TService, Task> onActivate)
        {
            ServiceProvider = serviceProvider;
            OnActivate = onActivate;
        }

        public async Task Activate()
        {
            var service = ServiceProvider.GetRequiredService<TService>();
            await OnActivate(service).ConfigureAwait(false);
        }
    }
}