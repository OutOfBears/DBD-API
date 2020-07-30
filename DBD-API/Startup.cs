using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DBD_API.Modules;
using DBD_API.Modules.SPA;
using DBD_API.Modules.DbD.PakItems;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

using DBD_API.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace DBD_API
{
    public class Startup
    {
        private static readonly string _publicDataDir =
            Path.Combine(Directory.GetCurrentDirectory(), "data", "381210");

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

            if (!Directory.Exists(_publicDataDir))
                Directory.CreateDirectory(_publicDataDir);
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // logging
            services.AddLogging();
            services.AddCors();

            if(!string.IsNullOrEmpty(Configuration["redis:config"]))
                services.AddDistributedRedisCache(x =>
                {
                    x.Configuration = Configuration["redis:config"];
                    x.InstanceName = Configuration["redis:instance"];
                });
            else
                services.AddDistributedMemoryCache();

            // services
            services.AddSingleton<CacheService>();
            services.AddSingleton<DdbService>();
            services.AddSingleton<SteamService>();
            services.AddHostedService<SteamEventService>();
            services.AddHostedService<SteamDepotService>();

            // mvc
            services.AddControllers()
                .AddJsonOptions(x =>
                {
                    x.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    x.JsonSerializerOptions.Converters.Add(new TunableSerializer());
                });

            // spa
            services.AddSpaStaticFiles(options => options.RootPath = "ClientApp/dist");
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            else
                app.UseHsts();

            app.UseHttpsRedirection();

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(_publicDataDir),
                HttpsCompression = HttpsCompressionMode.Compress,
                RequestPath = "/data"
            });


            app.UseRouting();

            app.UseCors(builder =>
            {
                builder
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowAnyOrigin();
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapDefaultControllerRoute();
            });

            app.UseSpaStaticFiles();

            app.UseSpa(spa =>
            {
                spa.Options.SourcePath = "ClientApp";
                if (env.IsDevelopment())
                {
                    // Launch development server for Vue.js
                    spa.UseVueDevelopmentServer();
                }
            });
        }
    }
}
