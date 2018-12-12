using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Steamworks;
using System;
using System.IO;

namespace DBD_API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (!SteamAPI.Init())
            {
                throw new Exception("SteamAPI_Init failed!");
            }

            CreateWebHostBuilder(args)
                .Build()
                .Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.SetBasePath(Directory.GetCurrentDirectory());
                    config.AddJsonFile("config.json", optional: false, reloadOnChange: true);
                })
                .UseStartup<Startup>();
    }
}
