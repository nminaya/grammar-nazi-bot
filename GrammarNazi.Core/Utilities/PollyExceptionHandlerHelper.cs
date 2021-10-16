using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GrammarNazi.Core.Utilities
{
    public static class PollyExceptionHandlerHelper
    {
        public static async Task HandleExceptionAndRetry<T>(Task action, ILogger logger, CancellationToken cancellationToken, int numberOfTimes = 3)
            where T : Exception
        {
            var handler = Policy.Handle<T>()
            .WaitAndRetryAsync(numberOfTimes, retryCount =>
            {
                var timeToWait = TimeSpan.FromSeconds(15);
                logger.LogWarning($"{typeof(T).Name} captured: Retry #{retryCount}. Waiting 15 seconds.");

                return timeToWait;
            });

            var result = await handler.ExecuteAndCaptureAsync(_ => action, cancellationToken);

            if (result.Outcome == OutcomeType.Failure)
            {
                throw result.FinalException;
            }
        }
    }
}