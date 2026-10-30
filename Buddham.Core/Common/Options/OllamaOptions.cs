namespace Buddham.Core.Common.Options;

public class OllamaOptions
{
    public const string SectionName = "OllamaChat";
    public string BaseUrl { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string SystemPrompt { get; set; } = string.Empty;
    public bool Think { get; set; } = true;
}
