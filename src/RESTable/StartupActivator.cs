using System;
using Microsoft.Extensions.DependencyInjection;

namespace RESTable
{
    /// <summary>
    /// Used to register service types that should be loaded immediately on startup, to
    /// not delay error reporting to when they are activated.
    /// </summary>
    public interface IStartupActivator
    {
        void Activate();
    }

    /// <summary>
    /// Used to register service types that should be loaded immediately on startup, to
    /// not delay error reporting to when they are activated.
    /// </summary>
    public class StartupActivator<TService> : IStartupActivator where TService : class
    {
        private IServiceProvider ServiceProvider { get; }

        public StartupActivator(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        public void Activate()
        {
            ServiceProvider.GetRequiredService<TService>();
        }
    }
}