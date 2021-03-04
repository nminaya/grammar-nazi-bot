using GrammarNazi.App;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System;

Host.CreateDefaultBuilder(args)
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
    })
    .Build()
    .Run();