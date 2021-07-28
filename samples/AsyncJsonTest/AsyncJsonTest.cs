using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using RESTable.AspNetCore;

namespace AsyncJsonTest
{
    /// <summary>
    /// A simple RESTable application
    /// </summary>
    public class AsyncJsonTest
    {
        public static void Main(string[] args) => WebHost
            .CreateDefaultBuilder(args)
            .UseStartup<AsyncJsonTest>()
            .Build()
            .Run();

        public void ConfigureServices(IServiceCollection services) => services
            .AddRESTable()
            .AddHttpContextAccessor()
            .Configure<KestrelServerOptions>(o => o.AllowSynchronousIO = true);

        public void Configure(IApplicationBuilder app) => app
            .UseWebSockets()
            .UseRESTableAspNetCore();
    }
}