namespace Orchestrun.Core.Contracts.Events;

public sealed class NuGetDependencyInfo
{
    public required string Name { get; set; }
    public required string Version { get; set; }
    public bool IsDirectDependency { get; set; }
    public required string Type { get; set; }
    public string? Hash { get; set; }
}