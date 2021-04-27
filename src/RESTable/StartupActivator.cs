namespace RESTable
{
    /// <summary>
    /// Used to register service types that should be loaded immediately on startup, to
    /// not delay error reporting to when they are activated.
    /// </summary>
    public interface IStartupActivator { }

    /// <summary>
    /// Used to register service types that should be loaded immediately on startup, to
    /// not delay error reporting to when they are activated.
    /// </summary>
    public class StartupActivator<TService> : IStartupActivator where TService : class
    {
        private TService Dependency { get; }

        public StartupActivator(TService dependency)
        {
            Dependency = dependency;
        }
    }
}