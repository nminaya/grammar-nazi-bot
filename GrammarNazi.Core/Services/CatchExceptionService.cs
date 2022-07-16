using GrammarNazi.Core.Extensions;
using GrammarNazi.Domain.Enums;
using GrammarNazi.Domain.Exceptions;
using GrammarNazi.Domain.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using Telegram.Bot.Exceptions;

namespace GrammarNazi.Core.Services
{
    public class CatchExceptionService : ICatchExceptionService
    {
        private readonly ILogger<CatchExceptionService> _logger;
        private readonly IGithubService _githubService;

        public void HandleException(Exception exception, GithubIssueLabels githubIssueSection)
        {
            if (exception is TaskFailedException taskFailedException)
            {
                exception = taskFailedException.InnerException;
            }

            switch (exception)
            {
                case ApiRequestException apiRequestException:
                    HandleApiRequestException(apiRequestException);
                    break;

                case HttpRequestException httpRequestException:
                    HandleHttpRequestException(httpRequestException, githubIssueSection);
                    break;

                default:
                    HandleGeneralException(exception, githubIssueSection);
                    break;
            }
        }

        private void HandleHttpRequestException(HttpRequestException requestException, GithubIssueLabels githubIssueSection)
        {
            if (requestException.StatusCode == HttpStatusCode.BadGateway)
            {
                _logger.LogWarning("Bad Gateway", requestException);
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
            }
            else
            {
                _logger.LogError(apiRequestException, apiRequestException.Message);
            }
        }

        private void HandleGeneralException(Exception exception, GithubIssueLabels githubIssueSection)
        {
            var innerExceptions = exception.GetInnerExceptions();

            if (innerExceptions.Any(x => x.GetType() == typeof(SocketException) && x.Message.Contains("Connection reset by peer")))
            {
                // The server has reset the connection.
                _logger.LogWarning(exception, "Socket reseted.");

                return;
            }

            _logger.LogError(exception, exception.Message);

            // fire and forget
            _ = _githubService.CreateBugIssue($"Application Exception: {exception.Message}", exception, githubIssueSection);
        }
    }
}