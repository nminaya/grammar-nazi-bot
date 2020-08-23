using GrammarNazi.App.HostedServices;
using GrammarNazi.Core.Clients;
using GrammarNazi.Core.Repositories;
using GrammarNazi.Core.Services;
using GrammarNazi.Domain.Clients;
using GrammarNazi.Domain.Repositories;
using GrammarNazi.Domain.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using Telegram.Bot;

namespace GrammarNazi.App
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            // Hosted services
            services.AddHostedService<BotHostedService>();

            ConfigureDependencies(services);
        }

        private static void ConfigureDependencies(IServiceCollection services)
        {
            // Repository
            services.AddTransient(typeof(IRepository<>), typeof(InMemoryRepository<>));

            // Services
            services.AddSingleton<IFileService, FileService>();
            services.AddSingleton<IStringDiffService, StringDiffService>();
            services.AddTransient<ILanguageToolApiClient, LanguageToolApiClient>();
            services.AddTransient<IChatConfigurationService, ChatConfigurationService>();
            services.AddTransient<ILanguageService, NTextCatLanguageService>();
            services.AddTransient<IGrammarService, LanguageToolApiService>();
            services.AddTransient<IGrammarService, InternalFileGrammarService>();

            // Telegram client
            services.AddTransient<ITelegramBotClient>(_ =>
            {
                var apiKey = Environment.GetEnvironmentVariable("TELEGRAM_API_KEY");

                if (string.IsNullOrEmpty(apiKey))
                    throw new InvalidOperationException("Empty TELEGRAM_API_KEY");

                return new TelegramBotClient(apiKey);
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();

                // Return 200 if request is received
                endpoints.MapGet("/", async context =>
                {
                    context.Response.StatusCode = 200;
                    await context.Response.WriteAsync("App Running");
                });
            });
        }
    }
}