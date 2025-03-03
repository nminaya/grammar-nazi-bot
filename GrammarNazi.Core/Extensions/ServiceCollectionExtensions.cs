using GrammarNazi.Domain.BotCommands;
using GrammarNazi.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NTextCat;
using System;
using System.Linq;
using System.Reflection;

namespace GrammarNazi.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNTextCatLanguageService(this IServiceCollection serviceCollection)
    {
        return serviceCollection.AddTransient<BasicProfileFactoryBase<RankedLanguageIdentifier>, RankedLanguageIdentifierFactory>();
    }

    public static IServiceCollection AddNamedHttpClients(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddHttpClient("datamuseApi", c => c.BaseAddress = new Uri("https://api.datamuse.com/"));
        serviceCollection.AddHttpClient("languageToolApi", c => c.BaseAddress = new Uri("https://languagetool.org/"));
        serviceCollection.AddHttpClient("yandexSpellerApi", c => c.BaseAddress = new Uri("https://speller.yandex.net/"));
        serviceCollection.AddHttpClient("sentimApi", c => c.BaseAddress = new Uri("https://sentim-api.herokuapp.com/"));
        serviceCollection.AddHttpClient("geminiApi", c => c.BaseAddress = new Uri("https://generativelanguage.googleapis.com/"));

        var meaningCloudSettings = serviceCollection.BuildServiceProvider().GetService<IOptions<MeaningCloudSettings>>().Value;

        serviceCollection.AddHttpClient("meaninCloudSentimentAnalysisApi", c => c.BaseAddress = new Uri(meaningCloudSettings.MeaningCloudSentimentHostUrl));
        serviceCollection.AddHttpClient("meaninCloudLanguageApi", c => c.BaseAddress = new Uri(meaningCloudSettings.MeaningCloudLanguageHostUrl));

        return serviceCollection;
    }

    public static IServiceCollection AddSqliteDbContext(this IServiceCollection serviceCollection, string connectionString)
    {
        serviceCollection.AddDbContext<GrammarNaziContext>(options => options.UseSqlite(connectionString));
        serviceCollection.AddTransient<DbContext, GrammarNaziContext>();

        return serviceCollection;
    }

    public static IServiceCollection AddSqlServerDbContext(this IServiceCollection serviceCollection, string connectionString)
    {
        serviceCollection.AddDbContext<GrammarNaziContext>(options => options.UseSqlServer(connectionString));
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
        return AddTransientInstancesOf<IDiscordBotCommand>(serviceCollection);
    }

    public static IServiceCollection AddTelegramBotCommands(this IServiceCollection serviceCollection)
    {
        // All ITelegramBotCommand classes in the current Assembly
        return AddTransientInstancesOf<ITelegramBotCommand>(serviceCollection);
    }

    private static IServiceCollection AddTransientInstancesOf<T>(IServiceCollection serviceCollection)
    {
        var type = typeof(T);

        var commandClassTypes = Assembly
            .GetExecutingAssembly()
            .GetTypes()
            .Where(v => v.IsAssignableTo(type));

        foreach (var commandClassType in commandClassTypes)
        {
            serviceCollection.AddTransient(type, commandClassType);
        }

        return serviceCollection;
    }
}