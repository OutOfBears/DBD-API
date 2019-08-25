using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

using DBD_API.Services;
using Joveler.Compression.ZLib;
using Microsoft.Extensions.Configuration;

namespace DBD_API
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        // Configures the class with the config
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            ZLibInit.GlobalInit("x64/zlibwapi.dll");

            Action<string> ensureConfig = (name) =>
            {
                if(string.IsNullOrEmpty(configuration[name]))
                    throw new Exception($"Failed to get setting {name}");
            };

            ensureConfig("steam_user");
            ensureConfig("steam_pass");
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // services
            services.AddSingleton<SteamService>();
            services.AddSingleton<DdbService>();
            services.AddHostedService<SteamEventService>();

            // mvc
            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc(routes =>
            {
                routes.MapRoute("default", "{controller=API}/{action=Index}/{id?}");
            });
        }
    }
}
