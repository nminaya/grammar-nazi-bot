﻿using GrammarNazi.Domain.BotCommands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NTextCat;
using System.Linq;
using System.Reflection;

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

        public static IServiceCollection AddDiscordBotCommands(this IServiceCollection serviceCollection)
        {
            // All IDiscordBotCommand classes in the current Assembly
            var commandClassTypes = Assembly
                .GetExecutingAssembly()
                .GetTypes()
                .Where(v => v.IsAssignableTo(typeof(IDiscordBotCommand)));

            foreach (var commandClassType in commandClassTypes)
            {
                serviceCollection.AddTransient(typeof(IDiscordBotCommand), commandClassType);
            }

            return serviceCollection;
        }
    }
}