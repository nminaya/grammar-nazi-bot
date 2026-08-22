using GrammarNazi.Domain.BotCommands;
using GrammarNazi.Domain.Constants;
using GrammarNazi.Domain.Entities.Settings;
using GrammarNazi.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NTextCat;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using System.Net;
using System.Reflection;

namespace GrammarNazi.Core.Extensions;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection serviceCollection)
    {
        public IServiceCollection AddNTextCatLanguageService()
        {
            return serviceCollection.AddTransient<BasicProfileFactoryBase<RankedLanguageIdentifier>, RankedLanguageIdentifierFactory>();
        }

        public IServiceCollection AddNamedHttpClients()
        {
            serviceCollection.AddHttpClient("datamuseApi", c => { c.BaseAddress = new Uri("https://api.datamuse.com/"); c.Timeout = TimeSpan.FromSeconds(30); });
            serviceCollection.AddHttpClient("languageToolApi", c => { c.BaseAddress = new Uri("https://languagetool.org/"); c.Timeout = TimeSpan.FromSeconds(30); });
            serviceCollection.AddHttpClient("yandexSpellerApi", c => { c.BaseAddress = new Uri("https://speller.yandex.net/"); c.Timeout = TimeSpan.FromSeconds(30); });
            serviceCollection.AddHttpClient("sentimApi", c => { c.BaseAddress = new Uri("https://sentim-api.herokuapp.com/"); c.Timeout = TimeSpan.FromSeconds(30); });

            serviceCollection.AddSingleton<GroqResilienceHolder>();
            serviceCollection.AddSingleton<CerebrasResilienceHolder>();
            serviceCollection.AddSingleton<GeminiResilienceHolder>();

            serviceCollection.AddHttpClient("groqApi", c => { c.BaseAddress = new Uri("https://api.groq.com/"); c.Timeout = TimeSpan.FromSeconds(30); })
                .AddHttpMessageHandler(sp =>
                {
                    var holder = sp.GetRequiredService<GroqResilienceHolder>();
                    return new ApiResilienceHandler(holder.Limiter, holder.Pipeline);
                });

            serviceCollection.AddHttpClient("cerebrasApi", c => { c.BaseAddress = new Uri("https://api.cerebras.ai/"); c.Timeout = TimeSpan.FromSeconds(30); })
                .AddHttpMessageHandler(sp =>
                {
                    var holder = sp.GetRequiredService<CerebrasResilienceHolder>();
                    return new ApiResilienceHandler(holder.Limiter, holder.Pipeline);
                });

            serviceCollection.AddHttpClient("geminiApi", c => { c.BaseAddress = new Uri("https://generativelanguage.googleapis.com/"); c.Timeout = TimeSpan.FromSeconds(30); })
                .AddHttpMessageHandler(sp =>
                {
                    var holder = sp.GetRequiredService<GeminiResilienceHolder>();
                    return new ApiResilienceHandler(holder.Limiter, holder.Pipeline);
                });

            var provider = serviceCollection.BuildServiceProvider();
            var meaningCloudSettings = provider.GetService<IOptions<MeaningCloudSettings>>().Value;

            serviceCollection.AddHttpClient("meaninCloudSentimentAnalysisApi", c => { c.BaseAddress = new Uri(meaningCloudSettings.MeaningCloudSentimentHostUrl); c.Timeout = TimeSpan.FromSeconds(30); });
            serviceCollection.AddHttpClient("meaninCloudLanguageApi", c => { c.BaseAddress = new Uri(meaningCloudSettings.MeaningCloudLanguageHostUrl); c.Timeout = TimeSpan.FromSeconds(30); });

            return serviceCollection;
        }

        public IServiceCollection AddSqliteDbContext(string connectionString)
        {
            serviceCollection.AddDbContext<GrammarNaziContext>(options => options.UseSqlite(connectionString));
            serviceCollection.AddTransient<DbContext, GrammarNaziContext>();

            return serviceCollection;
        }

        public IServiceCollection AddSqlServerDbContext(string connectionString)
        {
            serviceCollection.AddDbContext<GrammarNaziContext>(options => options.UseSqlServer(connectionString));
            serviceCollection.AddTransient<DbContext, GrammarNaziContext>();

            return serviceCollection;
        }

        public void EnsureDatabaseCreated()
        {
            using var scope = serviceCollection.BuildServiceProvider().CreateScope();
            var context = scope.ServiceProvider.GetService<DbContext>();
            context.Database.EnsureCreated();
        }

        public IServiceCollection AddDiscordBotCommands()
        {
            // All IDiscordBotCommand classes in the current Assembly
            return AddTransientInstancesOf<IDiscordBotCommand>(serviceCollection);
        }

        public IServiceCollection AddTelegramBotCommands()
        {
            // All ITelegramBotCommand classes in the current Assembly
            return AddTransientInstancesOf<ITelegramBotCommand>(serviceCollection);
        }
    }

    internal class ApiResilienceHolder(int requestsPerMinute, int maxRetries)
    {
        public SlidingWindowRateLimiter Limiter { get; } = new(requestsPerMinute, TimeSpan.FromMinutes(1));
        public ResiliencePipeline<HttpResponseMessage> Pipeline { get; } = CreateApiResiliencePipeline(maxRetries);
    }

    internal class GroqResilienceHolder() : ApiResilienceHolder(Defaults.GroqRequestsPerMinute, Defaults.GroqMaxRetries);
    internal class CerebrasResilienceHolder() : ApiResilienceHolder(Defaults.CerebrasRequestsPerMinute, Defaults.CerebrasMaxRetries);
    internal class GeminiResilienceHolder() : ApiResilienceHolder(Defaults.GeminiRequestsPerMinute, Defaults.GeminiMaxRetries);

    internal static ResiliencePipeline<HttpResponseMessage> CreateApiResiliencePipeline(int maxRetries)
    {
        var builder = new ResiliencePipelineBuilder<HttpResponseMessage>();

        // 1. Retry (outermost strategy in pipeline)
        builder.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
        {
            ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                .HandleResult(r => r.StatusCode is HttpStatusCode.ServiceUnavailable
                                               or HttpStatusCode.BadGateway
                                               or HttpStatusCode.GatewayTimeout)
                .Handle<HttpRequestException>(ex => ex.HttpRequestError is HttpRequestError.NameResolutionError
                                                                      or HttpRequestError.ConnectionError
                                                                      or HttpRequestError.ResponseEnded),
            MaxRetryAttempts = maxRetries,
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            Delay = TimeSpan.FromSeconds(1),
            MaxDelay = TimeSpan.FromSeconds(4)
        });

        // 2. Circuit breaker (innermost strategy in pipeline)
        builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
        {
            ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                .HandleResult(r => r.StatusCode == HttpStatusCode.TooManyRequests),
            BreakDuration = TimeSpan.FromSeconds(60),
            SamplingDuration = TimeSpan.FromSeconds(60),
            FailureRatio = 0.5,
            MinimumThroughput = 10
        });

        return builder.Build();
    }

    internal class ApiResilienceHandler(SlidingWindowRateLimiter rateLimiter, ResiliencePipeline<HttpResponseMessage> pipeline) : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return pipeline.ExecuteAsync(async ct =>
            {
                if (!rateLimiter.TryAcquire())
                {
                    throw new ExternalApiRateLimitException("API rate limit reached by client rate limiter.");
                }

                return await base.SendAsync(request, ct);
            }, cancellationToken).AsTask();
        }
    }

    internal class SlidingWindowRateLimiter(int permitLimit, TimeSpan window)
    {
        private readonly object _lock = new();
        private readonly Queue<DateTime> _timestamps = [];
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
