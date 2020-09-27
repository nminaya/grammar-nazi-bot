namespace GrammarNazi.Domain.Entities.Configs
{
    public class TwitterBotSettings
    {
        public string BotUsername { get; set; }
        public int TimelineFirstLoadPageSize { get; set; }
        public int PublishTweetDelayMilliseconds { get; set; }
        public int HostedServiceIntervalMilliseconds { get; set; }
    }
}