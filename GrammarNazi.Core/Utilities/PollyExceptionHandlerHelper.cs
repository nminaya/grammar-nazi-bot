using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GrammarNazi.Core.Utilities
{
    public static class PollyExceptionHandlerHelper
    {
        public static AsyncPolicy GetSqlExceptionHandler(ILogger logger)
        {
            return Policy.Handle<Exception>()
            .WaitAndRetryAsync(3, retryCount =>
            {
                var timeToWait = TimeSpan.FromSeconds(5);
                logger.LogWarning($"SqlException captured: Retry #{retryCount}. Waiting 25 seconds.");

                return timeToWait;
            });
        }

        public static async Task HandleExceptionAndRetry<T>(int numberOfTimes, ILogger logger, Task action, CancellationToken cancellationToken)
            where T : Exception
        {
            // TODO: Implement this method
        }
    }
}