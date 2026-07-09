using System.Text.Json.Serialization;

namespace Orchestrun.Core.Contracts.Events;

public sealed class NuGetDependenciesPublished
{
    [JsonPropertyName("serviceName")] 
    public required string ServiceName { get; init; }

    [JsonPropertyName("runtimeTarget")] 
    public required string RuntimeTarget { get; init; }

    [JsonPropertyName("dependencies")] 
    public required List<NuGetDependencyInfo> Dependencies { get; init; }
}