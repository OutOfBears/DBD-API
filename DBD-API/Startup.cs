using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

using DBD_API.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace DBD_API
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        // Configures the class with the config
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

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
            services.AddSingleton<DdbService>();
            services.AddSingleton<SteamService>();
            services.AddHostedService<SteamEventService>();
            services.AddHostedService<SteamDepotService>();

            // mvc
            services.AddControllers();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapControllerRoute("default", "{controller=API}/{action=Index}/{id?}");
            });
        }
    }
}
