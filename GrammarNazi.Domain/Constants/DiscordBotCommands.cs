namespace GrammarNazi.Domain.Constants
{
    public static class DiscordBotCommands
    {
        public const string Prefix = "!";

        public const string Start = Prefix + "start";
        public const string Help = Prefix + "help";
        public const string Settings = Prefix + "settings";
        public const string SetAlgorithm = Prefix + "set_algorithm";
        public const string Language = Prefix + "lang";
        public const string Stop = Prefix + "stop";
        public const string HideDetails = Prefix + "hide_details";
        public const string ShowDetails = Prefix + "show_details";
        public const string Tolerant = Prefix + "tolerant";
        public const string Intolerant = Prefix + "intolerant";
        public const string WhiteList = Prefix + "whitelist";
        public const string AddWhiteList = Prefix + "add_whitelist";
        public const string RemoveWhiteList = Prefix + "remove_whitelist";
    }
}