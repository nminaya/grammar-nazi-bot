using GrammarNazi.Domain.BotCommands;
using GrammarNazi.Domain.Entities.Settings;
using GrammarNazi.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NTextCat;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace GrammarNazi.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNTextCatLanguageService(this IServiceCollection serviceCollection)
    {
        return serviceCollection.AddTransient<BasicProfileFactoryBase<RankedLanguageIdentifier>, RankedLanguageIdentifierFactory>();
    }

    public static IServiceCollection AddNamedHttpClients(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddHttpClient("datamuseApi", c => { c.BaseAddress = new Uri("https://api.datamuse.com/"); c.Timeout = TimeSpan.FromSeconds(30); });
        serviceCollection.AddHttpClient("languageToolApi", c => { c.BaseAddress = new Uri("https://languagetool.org/"); c.Timeout = TimeSpan.FromSeconds(30); });
        serviceCollection.AddHttpClient("yandexSpellerApi", c => { c.BaseAddress = new Uri("https://speller.yandex.net/"); c.Timeout = TimeSpan.FromSeconds(30); });
        serviceCollection.AddHttpClient("sentimApi", c => { c.BaseAddress = new Uri("https://sentim-api.herokuapp.com/"); c.Timeout = TimeSpan.FromSeconds(30); });
        serviceCollection.AddHttpClient("geminiApi", c => { c.BaseAddress = new Uri("https://generativelanguage.googleapis.com/"); c.Timeout = TimeSpan.FromSeconds(30); });

        var provider = serviceCollection.BuildServiceProvider();
        var groqSettings = provider.GetService<IOptions<GroqApiSettings>>()?.Value;
        var cerebrasSettings = provider.GetService<IOptions<CerebrasApiSettings>>()?.Value;

        int groqRpm = groqSettings?.RequestsPerMinute ?? 25;
        int groqRetries = groqSettings?.MaxRetries ?? 2;
        int cerebrasRpm = cerebrasSettings?.RequestsPerMinute ?? 25;
        int cerebrasRetries = cerebrasSettings?.MaxRetries ?? 2;

        var groqPipeline = CreateApiResiliencePipeline(groqRetries);
        var cerebrasPipeline = CreateApiResiliencePipeline(cerebrasRetries);

        serviceCollection.AddHttpClient("groqApi", c => { c.BaseAddress = new Uri("https://api.groq.com/"); c.Timeout = TimeSpan.FromSeconds(30); })
            .AddHttpMessageHandler(() => new ApiResilienceHandler(groqRpm, groqPipeline));

        serviceCollection.AddHttpClient("cerebrasApi", c => { c.BaseAddress = new Uri("https://api.cerebras.ai/"); c.Timeout = TimeSpan.FromSeconds(30); })
            .AddHttpMessageHandler(() => new ApiResilienceHandler(cerebrasRpm, cerebrasPipeline));

        var meaningCloudSettings = provider.GetService<IOptions<MeaningCloudSettings>>().Value;

        serviceCollection.AddHttpClient("meaninCloudSentimentAnalysisApi", c => { c.BaseAddress = new Uri(meaningCloudSettings.MeaningCloudSentimentHostUrl); c.Timeout = TimeSpan.FromSeconds(30); });
        serviceCollection.AddHttpClient("meaninCloudLanguageApi", c => { c.BaseAddress = new Uri(meaningCloudSettings.MeaningCloudLanguageHostUrl); c.Timeout = TimeSpan.FromSeconds(30); });

        return serviceCollection;
    }

    private static ResiliencePipeline<HttpResponseMessage> CreateApiResiliencePipeline(int maxRetries)
    {
        var builder = new ResiliencePipelineBuilder<HttpResponseMessage>();

        // 1. Circuit breaker (outermost strategy in Polly pipeline)
        builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
        {
            ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                .HandleResult(r => r.StatusCode == HttpStatusCode.TooManyRequests),
            BreakDuration = TimeSpan.FromSeconds(60),
            SamplingDuration = TimeSpan.FromSeconds(30),
            FailureRatio = 0.5,
            MinimumThroughput = 2
        });

        // 2. Retry (innermost strategy)
        builder.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
        {
            ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                .HandleResult(r =>
                {
                    if (r.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        var retryAfter = r.Headers.RetryAfter;
                        if (retryAfter != null)
                        {
                            TimeSpan? delta = retryAfter.Delta;
                            if (!delta.HasValue && retryAfter.Date.HasValue)
                            {
                                delta = retryAfter.Date.Value - DateTimeOffset.UtcNow;
                            }
                            if (delta.HasValue && delta.Value > TimeSpan.FromSeconds(4))
                            {
                                return false; // Retry-After asks for > 4s, give up immediately
                            }
                        }
                        return true;
                    }
                    return r.StatusCode == HttpStatusCode.ServiceUnavailable
                        || r.StatusCode == HttpStatusCode.BadGateway
                        || r.StatusCode == HttpStatusCode.GatewayTimeout;
                }),
            MaxRetryAttempts = maxRetries,
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            Delay = TimeSpan.FromSeconds(1),
            MaxDelay = TimeSpan.FromSeconds(4),
            DelayGenerator = args =>
            {
                var result = args.Outcome.Result;
                if (result?.Headers?.RetryAfter != null)
                {
                    var retryAfter = result.Headers.RetryAfter;
                    TimeSpan? delta = retryAfter.Delta;
                    if (!delta.HasValue && retryAfter.Date.HasValue)
                    {
                        delta = retryAfter.Date.Value - DateTimeOffset.UtcNow;
                    }
                    if (delta.HasValue)
                    {
                        var delay = delta.Value > TimeSpan.FromSeconds(4) ? TimeSpan.FromSeconds(4) : delta.Value;
                        if (delay < TimeSpan.Zero) delay = TimeSpan.Zero;
                        return ValueTask.FromResult<TimeSpan?>(delay);
                    }
                }
                return ValueTask.FromResult<TimeSpan?>(null);
            }
        });

        return builder.Build();
    }

    private class ApiResilienceHandler(int requestsPerMinute, ResiliencePipeline<HttpResponseMessage> pipeline) : DelegatingHandler
    {
        private readonly SlidingWindowRateLimiter _rateLimiter = new(requestsPerMinute, TimeSpan.FromMinutes(1));

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (!_rateLimiter.TryAcquire())
            {
                throw new ExternalApiRateLimitException("API rate limit reached by client rate limiter.");
            }

            return await pipeline.ExecuteAsync(async ct => await base.SendAsync(request, ct), cancellationToken);
        }
    }

    private class SlidingWindowRateLimiter(int permitLimit, TimeSpan window)
    {
        private readonly object _lock = new();
        private readonly Queue<DateTime> _timestamps = new();
        private readonly int _permitLimit = permitLimit > 0 ? permitLimit : 25;

        public bool TryAcquire()
        {
            lock (_lock)
            {
                var now = DateTime.UtcNow;
                while (_timestamps.Count > 0 && now - _timestamps.Peek() > window)
                {
                    _timestamps.Dequeue();
                }

                if (_timestamps.Count >= _permitLimit)
                {
                    return false;
                }

                _timestamps.Enqueue(now);
                return true;
            }
        }
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
