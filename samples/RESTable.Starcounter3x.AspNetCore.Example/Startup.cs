using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using RESTable.AspNetCore;

namespace RESTable.Example
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services) => services
            .AddStarcounter("Database=./database")
            .AddJsonProvider()
            .AddStarcounterProvider()
            .AddRESTable()
            .AddHttpContextAccessor();
        
        public void Configure(IApplicationBuilder app) => app
            .UseWebSockets()
            .UseRESTableAspNetCore();
    }
}