namespace Buddham.Core.Common.Options;

public class OllamaChatOptions
{
    public const string SectionName = "OllamaChat";
    public string BaseUrl { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
}
