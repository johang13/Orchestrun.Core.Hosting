using MassTransit;
using Microsoft.Extensions.Logging;

namespace Orchestrun.Core.Hosting.TestConsole;

public sealed class TestConsumer(ILogger<TestConsumer> logger) : IConsumer<TestEvent>
{
    public Task Consume(ConsumeContext<TestEvent> context)
    {
        logger.LogDebug("Consumed test - {0}", context.Message);
        return Task.CompletedTask; 
    }
}