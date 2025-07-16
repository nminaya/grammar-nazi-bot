﻿using Discord.Net;
using GrammarNazi.Core.Extensions;
using GrammarNazi.Domain.Enums;
using GrammarNazi.Domain.Exceptions;
using GrammarNazi.Domain.Services;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
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
                case GeminiServiceUnavailableException:
                    _logger.LogWarning(exception, exception.Message);
                    break;

                default:
                    HandleGeneralException(exception, githubIssueSection);
                    break;
            }
        }

        private void HandleRequestException(RequestException requestException, GithubIssueLabels githubIssueSection)
        {
            if (requestException.GetInnerExceptions().Any(x => x.Message?.ContainsAny("Operation canceled", "response ended prematurely") == true))
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

            HandleGeneralException(httpException, githubIssueSection);
        }

        private void HandleSqlException(SqlException sqlException, GithubIssueLabels githubIssueSection)
        {
            if (sqlException.Message.Contains("SHUTDOWN"))
            {
                _logger.LogWarning(sqlException, "Sql Server shutdown in progress");
                return;
            }

            HandleGeneralException(sqlException, githubIssueSection);
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
                _logger.LogWarning(exception, "Socket reseted.");

                return;
            }

            _logger.LogError(exception, message);

            // fire and forget
            _ = _githubService.CreateBugIssue($"Application Exception: {message}", exception, githubIssueSection);
        }
    }
}