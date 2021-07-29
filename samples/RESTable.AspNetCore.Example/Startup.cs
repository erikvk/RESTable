using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RESTable.Requests;
using RESTable.Resources;
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
        public void ConfigureServices(IServiceCollection services) => services
            .AddHttpContextAccessor()
            .AddSingleton<IEntityResourceProvider>(new SQLiteEntityResourceProvider("./database"))
            .AddJson()
            .AddExcelProvider()
            .AddSingleton<IEntityTypeContractResolver, MyStringPropertiesResolver>()
            .AddRESTable()
            .AddMvc(o => o.EnableEndpointRouting = false);

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, RootClient rootClient)
        {
            app.UseMvcWithDefaultRoute();
            app.UseWebSockets();

            var rootContext = new RESTableContext(rootClient, app.ApplicationServices);
            var personPostRequest = rootContext
                .CreateRequest<Person>()
                .WithMethod(Method.POST)
                .WithBody(new object[]
                {
                    new
                    {
                        FirstName = "Sarah",
                        LastName = "Connor",
                        DateOfBirth = 19870304,
                        Interests = new[] {"Survival"}
                    },
                    new
                    {
                        FirstName = "Jordan",
                        LastName = "Belfort",
                        DateOfBirth = 19880119,
                        Interests = new[] {"Money", "Drugs"}
                    },
                    new
                    {
                        FirstName = "Darth",
                        LastName = "Vader",
                        Interests = new[] {"Finding droids", "Darkness"}
                    },
                    new
                    {
                        FirstName = "Luke",
                        LastName = "Skywalker",
                        Interests = new[] {"Destiny", "Forces"}
                    }
                });
            using var result = personPostRequest.GetResult().Result;
        }
    }
}