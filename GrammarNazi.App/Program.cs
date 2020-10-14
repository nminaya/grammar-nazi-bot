using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System;

namespace GrammarNazi.App
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                    .ConfigureWebHostDefaults(webBuilder =>
                    {
                        webBuilder.UseStartup<Startup>();
                        webBuilder.UseKestrel((_, options) =>
                        {
                            var port = Environment.GetEnvironmentVariable("PORT");
                            if (!string.IsNullOrEmpty(port))
                            {
                                options.ListenAnyIP(int.Parse(port));
                            }
                        });
                    });
        }
    }
}