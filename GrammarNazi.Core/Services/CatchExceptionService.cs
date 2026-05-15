using Discord.Net;
using GrammarNazi.Core.Extensions;
using GrammarNazi.Core.Utilities;
using GrammarNazi.Domain.Enums;
using GrammarNazi.Domain.Exceptions;
using GrammarNazi.Domain.Services;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Net.Sockets;
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

        public async Task HandleException(Exception exception, GithubIssueLabels githubIssueSection)
        {
            if (exception is TaskFailedException taskFailedException)
            {
                exception = taskFailedException.InnerException;
            }
            else if (exception is AggregateException aggregateException)
            {
                exception = aggregateException.Flatten().InnerException;
            }

            switch (exception)
            {
                case ApiRequestException apiRequestException:
                    HandleApiRequestException(apiRequestException);
                    break;

                case HttpRequestException httpRequestException:
                    await HandleHttpRequestException(httpRequestException, githubIssueSection);
                    break;

                case SqlException sqlException:
                    await HandleSqlException(sqlException, githubIssueSection);
                    break;

                case HttpException httpException:
                    await HandleHttpException(httpException, githubIssueSection);
                    break;

                case TwitterException twitterException:
                    await HandleTwitterException(twitterException, githubIssueSection);
                    break;

                case RequestException requestException:
                    await HandleRequestException(requestException, githubIssueSection);
                    break;
                case GeminiServiceUnavailableException:
                    _logger.LogWarning(exception, exception.Message);
                    break;

                default:
                    await HandleGeneralException(exception, githubIssueSection);
                    break;
            }
        }

        private async Task HandleRequestException(RequestException requestException, GithubIssueLabels githubIssueSection)
        {
            var isTransient = requestException.Message.Contains("timed out", StringComparison.OrdinalIgnoreCase)
                || requestException.GetInnerExceptions().Any(x => x.Message?.ContainsAny(StringComparison.OrdinalIgnoreCase, "Operation canceled", "task was canceled", "response ended prematurely") == true);

            if (isTransient)
            {
                _logger.LogWarning(requestException, requestException.Message);
                return;
            }

            await HandleGeneralException(requestException, githubIssueSection);
        }

        private async Task HandleTwitterException(TwitterException twitterException, GithubIssueLabels githubIssueSection)
        {
            if (twitterException.TwitterDescription.Contains("Try again later") || twitterException.TwitterDescription.Contains("Timeout limit"))
            {
                _logger.LogWarning(twitterException, twitterException.TwitterDescription);
                return;
            }

            await HandleGeneralException(twitterException, githubIssueSection);
        }

        private async Task HandleHttpException(HttpException httpException, GithubIssueLabels githubIssueSection)
        {
            if (httpException.Message.ContainsAny("50013", "50001", "Forbidden", "160002") 
                || httpException.HttpCode == HttpStatusCode.BadRequest)
            {
                _logger.LogWarning(httpException, httpException.Message);
                return;
            }

            await HandleGeneralException(httpException, githubIssueSection);
        }

        private async Task HandleSqlException(SqlException sqlException, GithubIssueLabels githubIssueSection)
        {
            if (sqlException.Message.Contains("SHUTDOWN"))
            {
                _logger.LogWarning(sqlException, "Sql Server shutdown in progress");
                return;
            }

            if (SqlExceptionHelper.IsTransient(sqlException))
            {
                await HandleTransientSqlException(sqlException, githubIssueSection);
                return;
            }

            await HandleGeneralException(sqlException, githubIssueSection);
        }

        private async Task HandleTransientSqlException(SqlException sqlException, GithubIssueLabels githubIssueSection)
        {
            _logger.LogWarning(sqlException, $"Transient SQL error: {sqlException.Message}");

            await _githubService.CreateBugIssue($"Transient SQL Exception: {sqlException.Message}", sqlException, githubIssueSection);
        }

        private async Task HandleHttpRequestException(HttpRequestException requestException, GithubIssueLabels githubIssueSection)
        {
            if (requestException.StatusCode == HttpStatusCode.BadGateway)
            {
                _logger.LogWarning(requestException, "Bad Gateway");
                return;
            }

            await HandleGeneralException(requestException, githubIssueSection);
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

        private async Task HandleGeneralException(Exception exception, GithubIssueLabels githubIssueSection)
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

            await _githubService.CreateBugIssue($"Application Exception: {message}", exception, githubIssueSection);
        }
    }
}