using System.ComponentModel;

namespace GrammarNazi.Domain.Enums;

public enum GithubIssueLabels
{
    [Description("telegram")]
    Telegram,

    [Description("twitter")]
    Twitter,

    [Description("discord")]
    Discord,

    [Description("production-bug")]
    ProductionBug
}
