using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using System;

namespace EJ2APIServices
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args)
        {
            var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";

            return WebHost.CreateDefaultBuilder(args)
                .UseUrls($"http://*:{port}")
                .UseStartup<Startup>()
                .Build();
        }
    }
}