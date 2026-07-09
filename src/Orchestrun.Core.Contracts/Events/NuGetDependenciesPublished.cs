using System.Text.Json.Serialization;

namespace Orchestrun.Core.Contracts.Events;

/// <summary>
/// Represents an event that captures the details of NuGet package dependencies
/// for a specific service, including the direct and transitive dependencies.
/// </summary>
public sealed class NuGetDependenciesPublished
{
    /// <summary>
    /// Identifies the service that published the dependencies.
    /// </summary>
    [JsonPropertyName("serviceName")] 
    public required string ServiceName { get; init; }

    /// <summary>
    /// Runtime target for which the dependencies were published.
    /// </summary>
    [JsonPropertyName("runtimeTarget")] 
    public required string RuntimeTarget { get; init; }

    /// <summary>
    /// List of NuGet dependencies published for the specified service and runtime target.
    /// </summary>
    [JsonPropertyName("dependencies")] 
    public required List<NuGetDependencyInfo> Dependencies { get; init; }
}