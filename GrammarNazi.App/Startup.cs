using GrammarNazi.App.HostedServices;
using GrammarNazi.Core.Clients;
using GrammarNazi.Core.Services;
using GrammarNazi.Domain.Clients;
using GrammarNazi.Domain.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GrammarNazi.App
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
            services.AddControllers();

            services.AddHostedService<BotHostedService>();

            services.AddSingleton<IFileService, FileService>();
            services.AddSingleton<IStringDiffService, StringDiffService>();
            services.AddTransient<ILanguageToolApiClient, LanguageToolApiClient>();
            //services.AddTransient<IGrammarService, InternalFileGrammarService>();
            services.AddTransient<IGrammarService, LanguageToolApiService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
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