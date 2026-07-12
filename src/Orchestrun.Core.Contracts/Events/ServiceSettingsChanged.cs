using System.Text.Json.Serialization;

namespace Orchestrun.Core.Contracts.Events;

public sealed class ServiceSettingsChanged
{
    [JsonPropertyName("targetServiceName")]
    public required string TargetServiceName { get; set; }

    [JsonPropertyName("isGlobal")]
    public bool IsGlobal { get; set; }

    [JsonPropertyName("key")]
    public required string Key { get; set; }

    [JsonPropertyName("value")]
    public required string Value { get; set; }
}