namespace GrammarNazi.Domain.Constants
{
    public static class DiscordBotCommands
    {
        public static readonly string Prefix = "!";

        public static readonly string Start = $"{Prefix}start";
        public static readonly string Help = $"{Prefix}help";
        public static readonly string Settings = $"{Prefix}settings";
        public static readonly string SetAlgorithm = $"{Prefix}set_algorithm";
        public static readonly string Language = $"{Prefix}lang";
        public static readonly string Stop = $"{Prefix}stop";
        public static readonly string HideDetails = $"{Prefix}hide_details";
        public static readonly string ShowDetails = $"{Prefix}show_details";
        public static readonly string Tolerant = $"{Prefix}tolerant";
        public static readonly string Intolerant = $"{Prefix}intolerant";
        public static readonly string WhiteList = $"{Prefix}whitelist";
        public static readonly string AddWhiteList = $"{Prefix}add_whitelist";
        public static readonly string RemoveWhiteList = $"{Prefix}remove_whitelist";
    }
}