namespace Orchestrun.Core.Contracts.Events;

/// <summary>
/// Dependency information for a NuGet package.
/// Used in the <see cref="NuGetDependenciesPublished"/> event.
/// </summary>
public sealed class NuGetDependencyInfo
{
    /// <summary>
    /// Name of the NuGet dependency.
    /// </summary>
    public required string Name { get; set; }
    
    /// <summary>
    /// Package version.
    /// </summary>
    public required string Version { get; set; }
    
    /// <summary>
    /// Indicates whether the dependency is a direct dependency of the service.
    /// </summary>
    public bool IsDirectDependency { get; set; }
    
    /// <summary>
    /// Type of the NuGet dependency (package, framework, etc.).
    /// </summary>
    public required string Type { get; set; }
    
    /// <summary>
    /// Hash of the NuGet dependency.
    /// </summary>
    public string? Hash { get; set; }
}