using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using RESTable.AspNetCore;

namespace RESTable.Example
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddStarcounter("Database=./database");
            services.AddJsonProvider();
            services.AddStarcounterProvider();
            services.AddExcelProvider();
            services.AddMvc(o => o.EnableEndpointRouting = false);
            services.AddHttpContextAccessor();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseMvcWithDefaultRoute();
            app.UseWebSockets();
            app.UseRESTableAspNetCore();
        }
    }
}