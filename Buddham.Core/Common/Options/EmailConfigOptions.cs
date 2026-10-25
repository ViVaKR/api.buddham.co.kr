namespace Buddham.Core.Common.Options;

public class EmailConfigOptions
{
    public const string SectionName = "EmailConfig";
    public required string Host { get; init; }
    public required int Port { get; init; }
    public required string From { get; init; }
    public required string Password { get; init; }
    public required string FromName { get; init; }
}
