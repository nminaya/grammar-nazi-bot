using Discord.WebSocket;
using Firebase.Database;
using GrammarNazi.App.HostedServices;
using GrammarNazi.Core.Clients;
using GrammarNazi.Core.Extensions;
using GrammarNazi.Core.Repositories;
using GrammarNazi.Core.Services;
using GrammarNazi.Core.Utilities;
using GrammarNazi.Domain.Clients;
using GrammarNazi.Domain.Entities.Configs;
using GrammarNazi.Domain.Entities.Settings;
using GrammarNazi.Domain.Repositories;
using GrammarNazi.Domain.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Octokit;
using System;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Tweetinvi;

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
            services.AddHostedService<TelegramBotHostedService>();
            //services.AddHostedService<DiscordBotHostedService>();
            //services.AddHostedService<TwitterBotMentionHostedService>();

            // Disable Twitter bot due to limitation/suspension
            //services.AddHostedService<TwitterBotHostedService>();
            ConfigureDependencies(services);
        }

        private void ConfigureDependencies(IServiceCollection services)
        {
            // Settings
            services.Configure<TwitterBotSettings>(Configuration.GetSection("AppSettings:TwitterBotSettings"));
            services.Configure<GithubSettings>(Configuration.GetSection("AppSettings:GithubSettings"));
            services.Configure<DiscordSettings>(d => d.Token = Environment.GetEnvironmentVariable("DISCORD_API_KEY"));
            services.Configure<MeaningCloudSettings>(m =>
            {
                m.MeaningCloudLanguageHostUrl = Configuration.GetSection("AppSettings:MeaningCloudSettings:MeaningCloudLanguageHostUrl").Value;
                m.MeaningCloudSentimentHostUrl = Configuration.GetSection("AppSettings:MeaningCloudSettings:MeaningCloudSentimentHostUrl").Value;
                m.Key = Environment.GetEnvironmentVariable("MEANING_CLOUD_API_KEY");
            });

            services.AddSqlServerDbContext(Environment.GetEnvironmentVariable("SQL_SERVER_CONNECTION_STRING"));

            // Repository
            services.AddTransient(typeof(IRepository<>), typeof(EFRepository<>));

            // Services
            services.AddSingleton<IFileService, FileService>();
            services.AddSingleton<IStringDiffService, StringDiffService>();
            services.AddTransient<ILanguageToolApiClient, LanguageToolApiClient>();
            services.AddTransient<IMeganingCloudLangApiClient, MeganingCloudLangApiClient>();
            services.AddTransient<IMeaningCloudSentimentAnalysisApiClient, MeaningCloudSentimentAnalysisApiClient>();
            services.AddTransient<IYandexSpellerApiClient, YandexSpellerApiClient>();
            services.AddTransient<IDatamuseApiClient, DatamuseApiClient>();
            services.AddTransient<ISentimApiClient, SentimApiClient>();
            services.AddTransient<IChatConfigurationService, ChatConfigurationService>();
            services.AddTransient<IScheduledTweetService, ScheduledTweetService>();
            services.AddTransient<ITwitterMentionLogService, TwitterMentionLogService>();
            services.AddTransient<ILanguageService, NTextCatLanguageService>();
            services.AddTransient<IGrammarService, LanguageToolApiService>();
            services.AddTransient<IGrammarService, InternalFileGrammarService>();
            services.AddTransient<IGrammarService, YandexSpellerApiService>();
            services.AddTransient<IGrammarService, DatamuseApiService>();
            services.AddTransient<ITwitterLogService, TwitterLogService>();
            services.AddTransient<IGithubService, GithubService>();
            services.AddTransient<ITelegramCommandHandlerService, TelegramCommandHandlerService>();
            services.AddTransient<ISentimentAnalysisService, MeaningCloudSentimentAnalysisService>();
            services.AddTransient<IDiscordChannelConfigService, DiscordChannelConfigService>();
            services.AddTransient<IDiscordCommandHandlerService, DiscordCommandHandlerService>();
            services.AddTransient<IUpdateHandler, TelegramUpdateHandler>();

            // Discord Bot Commands
            services.AddDiscordBotCommands();

            // Telegram Bot Commands
            services.AddTelegramBotCommands();

            // HttpClient
            services.AddHttpClient();
            services.AddNamedHttpClients();

            // NTextCatLanguageService
            services.AddNTextCatLanguageService();

            // Telegram client
            services.AddSingleton<ITelegramBotClient>(_ =>
            {
                var apiKey = Environment.GetEnvironmentVariable("TELEGRAM_API_KEY");

                if (string.IsNullOrEmpty(apiKey))
                    throw new InvalidOperationException("Empty TELEGRAM_API_KEY");

                return new TelegramBotClient(apiKey);
            });

            // Twitter client
            services.AddSingleton<ITwitterClient>(_ =>
            {
                var consumerKey = Environment.GetEnvironmentVariable("TWITTER_CONSUMER_KEY");
                var consumerSecret = Environment.GetEnvironmentVariable("TWITTER_CONSUMER_SECRET");
                var accessToken = Environment.GetEnvironmentVariable("TWITTER_ACCESS_TOKEN");
                var accessTokenSecret = Environment.GetEnvironmentVariable("TWITTER_ACCESS_TOKEN_SECRET");

                return new TwitterClient(consumerKey, consumerSecret, accessToken, accessTokenSecret);
            });

            // Firebase Client
            services.AddSingleton(_ =>
            {
                var databaseUrl = Environment.GetEnvironmentVariable("FIREBASE_DATABASE_URL");

                return new FirebaseClient(databaseUrl);
            });

            // Github Client
            services.AddSingleton<IGitHubClient>(s =>
            {
                var githubToken = Environment.GetEnvironmentVariable("GITHUB_ACCESS_TOKEN");
                var githubSettings = s.GetService<IOptions<GithubSettings>>().Value;

                return new GitHubClient(new ProductHeaderValue(githubSettings.Username))
                {
                    Credentials = new Credentials(githubToken)
                };
            });

            // Discord Client
            services.AddSingleton<BaseSocketClient, DiscordSocketClient>();
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