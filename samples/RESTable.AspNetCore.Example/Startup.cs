using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Excel;
using RESTable.OData;
using RESTable.ProtocolProviders;
using RESTable.SQLite;

namespace RESTable.Example
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IProtocolProvider, ODataProtocolProvider>();
            services.AddSingleton<IEntityResourceProvider>(new SQLiteEntityResourceProvider("./database"));
            services.AddExcelContentProvider();
            services.AddSingleton<IEntityTypeContractResolver, MyStringPropertiesResolver>();
            services.AddMvc(o => o.EnableEndpointRouting = false);
            services.AddHttpContextAccessor();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {
            app.UseMvcWithDefaultRoute();
            app.UseWebSockets();
            RESTableConfig.Init(services: app.ApplicationServices);

            RESTableContext.Root
                .CreateRequest<Person>(Method.POST)
                .WithBody(new object[]
                {
                    new
                    {
                        FirstName = "John",
                        LastName = "Stevens",
                        DateOfBirth = 19870304,
                        Interests = new[] {"Things", "Food"}
                    },
                    new
                    {
                        FirstName = "Jane",
                        LastName = "Stevens",
                        DateOfBirth = 19880119,
                        Interests = new[] {"Money", "Stuff"}
                    },
                    new
                    {
                        FirstName = "Darth",
                        LastName = "Vader",
                        Interests = new[] {"Droids", "Darkness"}
                    },
                    new
                    {
                        FirstName = "Luke",
                        LastName = "Skywalker",
                        Interests = new[] {"Destiny", "Forces"}
                    }
                })
                .Evaluate();
        }
    }
}