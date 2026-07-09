namespace Orchestrun.Core.Hosting.Options;

public sealed class ApplicationOptions
{
    internal const string SectionName = "Orchestrun";
    private string? ServiceSettingsManagerUrl { get; init; }
}