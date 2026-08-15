namespace GrammarNazi.Domain.Entities.Settings;

public class GroqApiSettings
{
    public string ApiKey { get; set; }
    public string Model { get; set; }
    public int RequestsPerMinute { get; set; } = 25;
    public int MaxRetries { get; set; } = 2;
}
