using GrammarNazi.Core.BotCommands.Discord;
using GrammarNazi.Domain.BotCommands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NTextCat;

namespace GrammarNazi.Core.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddNTextCatLanguageService(this IServiceCollection serviceCollection)
        {
            return serviceCollection.AddTransient<BasicProfileFactoryBase<RankedLanguageIdentifier>, RankedLanguageIdentifierFactory>();
        }

        public static IServiceCollection AddSqliteDbContext(this IServiceCollection serviceCollection, string connectionString)
        {
            serviceCollection.AddDbContext<GrammarNaziContext>(options => options.UseSqlite(connectionString));
            serviceCollection.AddTransient<DbContext, GrammarNaziContext>();

            return serviceCollection;
        }

        public static void EnsureDatabaseCreated(this IServiceCollection serviceCollection)
        {
            using var scope = serviceCollection.BuildServiceProvider().CreateScope();
            var context = scope.ServiceProvider.GetService<DbContext>();
            context.Database.EnsureCreated();
        }

        public static IServiceCollection AddDiscordCommands(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<IDiscordBotCommand, StartCommand>();
            serviceCollection.AddTransient<IDiscordBotCommand, SettingsCommand>();
            serviceCollection.AddTransient<IDiscordBotCommand, HelpCommand>();
            serviceCollection.AddTransient<IDiscordBotCommand, SetAlgorithmCommand>();
            serviceCollection.AddTransient<IDiscordBotCommand, LanguageCommand>();
            serviceCollection.AddTransient<IDiscordBotCommand, StopCommand>();
            serviceCollection.AddTransient<IDiscordBotCommand, HideDetailsCommand>();
            serviceCollection.AddTransient<IDiscordBotCommand, ShowDetailsCommand>();
            serviceCollection.AddTransient<IDiscordBotCommand, TolerantCommand>();
            serviceCollection.AddTransient<IDiscordBotCommand, IntolerantCommand>();
            serviceCollection.AddTransient<IDiscordBotCommand, WhiteListCommand>();

            return serviceCollection;
        }
    }
}