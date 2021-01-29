using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using RESTable.AspNetCore;
using RESTable.Excel;
using RESTable.Starcounter3x;

namespace RESTable.Example
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddStarcounter("Database=./database");
            services.AddStarcounterResourceProvider();
            services.AddStarcounterEntityTypeResolver();
            services.AddExcelContentProvider();
            services.AddMvc(o => o.EnableEndpointRouting = false);
            services.AddHttpContextAccessor();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseMvcWithDefaultRoute();
            app.UseRESTable();
        }
    }
}