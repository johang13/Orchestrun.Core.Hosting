namespace Orchestrun.Core.Hosting.TestConsole;

public sealed record TestEvent
{
    public string? Message { get; init; }
}