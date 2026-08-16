using Discord.Net;
using GrammarNazi.Core.Extensions;
using GrammarNazi.Core.Utilities;
using GrammarNazi.Domain.Enums;
using GrammarNazi.Domain.Exceptions;
using GrammarNazi.Domain.Services;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using Telegram.Bot.Exceptions;
using Tweetinvi.Exceptions;

namespace GrammarNazi.Core.Services
{
    public class CatchExceptionService : ICatchExceptionService
    {
        private readonly ILogger<CatchExceptionService> _logger;
        private readonly IGithubService _githubService;

        public CatchExceptionService(IGithubService githubService, ILogger<CatchExceptionService> logger)
        {
            _githubService = githubService;
            _logger = logger;
        }

        public void HandleException(Exception exception, GithubIssueLabels githubIssueSection)
        {
            if (exception is TaskFailedException taskFailedException)
            {
                exception = taskFailedException.InnerException;
            }

            if (IsTransientExternalApiFailure(exception))
            {
                _logger.LogWarning(exception, exception.Message);
                return;
            }

            switch (exception)
            {
                case ApiRequestException apiRequestException:
                    HandleApiRequestException(apiRequestException);
                    break;

                case HttpRequestException httpRequestException:
                    HandleHttpRequestException(httpRequestException, githubIssueSection);
                    break;

                case SqlException sqlException:
                    HandleSqlException(sqlException, githubIssueSection);
                    break;

                case HttpException httpException:
                    HandleHttpException(httpException, githubIssueSection);
                    break;

                case TwitterException twitterException:
                    HandleTwitterException(twitterException, githubIssueSection);
                    break;

                case RequestException requestException:
                    HandleRequestException(requestException, githubIssueSection);
                    break;

                case ExternalApiPermanentFailureException externalApiPermanentFailureException:
                    HandleExternalApiPermanentFailureException(externalApiPermanentFailureException, githubIssueSection);
                    break;

                case TaskCanceledException when exception.InnerException is TimeoutException:
                    _logger.LogWarning(exception, exception.Message);
                    break;

                default:
                    HandleGeneralException(exception, githubIssueSection);
                    break;
            }
        }

        /// <summary>
        /// Transient signals from an external API or from the Polly resilience pipeline guarding it.
        /// These are self-healing and must never open a production bug issue.
        /// Checked against the whole inner-exception chain because exceptions raised inside the
        /// HttpClient handler chain may surface wrapped.
        /// </summary>
        private static bool IsTransientExternalApiFailure(Exception exception)
        {
            return exception is ExternalApiRateLimitException or ExternalApiUnavailableException or BrokenCircuitException
                || exception.GetInnerExceptions().Any(x => x is ExternalApiRateLimitException or ExternalApiUnavailableException or BrokenCircuitException);
        }

        private void HandleRequestException(RequestException requestException, GithubIssueLabels githubIssueSection)
        {
            var isTransient = requestException.Message.Contains("timed out", StringComparison.OrdinalIgnoreCase)
                || requestException.GetInnerExceptions().Any(x => x.Message?.ContainsAny(StringComparison.OrdinalIgnoreCase, "Operation canceled", "task was canceled", "response ended prematurely") == true);

            if (isTransient)
            {
                _logger.LogWarning(requestException, requestException.Message);
                return;
            }

            HandleGeneralException(requestException, githubIssueSection);
        }

        private void HandleTwitterException(TwitterException twitterException, GithubIssueLabels githubIssueSection)
        {
            if (twitterException.TwitterDescription.Contains("Try again later") || twitterException.TwitterDescription.Contains("Timeout limit"))
            {
                _logger.LogWarning(twitterException, twitterException.TwitterDescription);
                return;
            }

            HandleGeneralException(twitterException, githubIssueSection);
        }

        private void HandleHttpException(HttpException httpException, GithubIssueLabels githubIssueSection)
        {
            if (httpException.Message.ContainsAny("50013", "50001", "Forbidden", "160002") 
                || httpException.HttpCode == HttpStatusCode.BadRequest)
            {
                _logger.LogWarning(httpException, httpException.Message);
                return;
            }

            if (httpException.HttpCode is HttpStatusCode.InternalServerError
                or HttpStatusCode.BadGateway
                or HttpStatusCode.ServiceUnavailable
                or HttpStatusCode.GatewayTimeout)
            {
                _logger.LogWarning(httpException, httpException.Message);
                return;
            }

            HandleGeneralException(httpException, githubIssueSection);
        }

        private void HandleExternalApiPermanentFailureException(ExternalApiPermanentFailureException exception, GithubIssueLabels githubIssueSection)
        {
            if (ExceptionThrottler.ShouldReport(exception.Message, TimeSpan.FromHours(24), threshold: 1))
            {
                _logger.LogError(exception, exception.Message);
                _ = _githubService.CreateBugIssue($"External API Failure: {exception.Message}", exception, githubIssueSection)
                    .ContinueWith(t => _logger.LogError(t.Exception, "Failed to create GitHub issue"), TaskContinuationOptions.OnlyOnFaulted);
            }
            else
            {
                _logger.LogWarning(exception, exception.Message);
            }
        }

        private void HandleSqlException(SqlException sqlException, GithubIssueLabels githubIssueSection)
        {
            if (sqlException.Message.Contains("SHUTDOWN"))
            {
                _logger.LogWarning(sqlException, "Sql Server shutdown in progress");
                return;
            }

            if (SqlExceptionHelper.IsTransient(sqlException))
            {
                HandleTransientSqlException(sqlException, githubIssueSection);
                return;
            }

            HandleGeneralException(sqlException, githubIssueSection);
        }

        private void HandleTransientSqlException(SqlException sqlException, GithubIssueLabels githubIssueSection)
        {
            _logger.LogWarning(sqlException, $"Transient SQL error: {sqlException.Message}");

            if (ExceptionThrottler.ShouldReport("SqlConnectivity", TimeSpan.FromMinutes(10), threshold: 10))
            {
                _ = _githubService.CreateBugIssue($"Transient SQL Exception: {sqlException.Message}", sqlException, githubIssueSection)
                    .ContinueWith(t => _logger.LogError(t.Exception, "Failed to create GitHub issue"), TaskContinuationOptions.OnlyOnFaulted);
            }
        }

        private void HandleHttpRequestException(HttpRequestException requestException, GithubIssueLabels githubIssueSection)
        {
            if (requestException.StatusCode == HttpStatusCode.BadGateway)
            {
                _logger.LogWarning(requestException, "Bad Gateway");
                return;
            }

            HandleGeneralException(requestException, githubIssueSection);
        }

        private void HandleApiRequestException(ApiRequestException apiRequestException)
        {
            var warningMessages = new[] { "bot was blocked by the user", "bot was kicked from the supergroup", "have no rights to send a message" };

            if (warningMessages.Any(x => apiRequestException.Message.Contains(x)))
            {
                _logger.LogWarning(apiRequestException.Message);
                return;
            }
         
            _logger.LogError(apiRequestException, apiRequestException.Message);
        }

        private void HandleGeneralException(Exception exception, GithubIssueLabels githubIssueSection)
        {
            var message = exception is TwitterException tEx ? tEx.TwitterDescription : exception.Message;

            var innerExceptions = exception.GetInnerExceptions();

            if (innerExceptions.Any(x => x.GetType() == typeof(SocketException) && x.Message.Contains("Connection reset by peer")))
            {
                // The server has reset the connection.
                _logger.LogWarning(exception, "Socket reset.");

                return;
            }

            _logger.LogError(exception, message);

            // fire and forget
            _ = _githubService.CreateBugIssue($"Application Exception: {message}", exception, githubIssueSection)
                .ContinueWith(t => _logger.LogError(t.Exception, "Failed to create GitHub issue"), TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}