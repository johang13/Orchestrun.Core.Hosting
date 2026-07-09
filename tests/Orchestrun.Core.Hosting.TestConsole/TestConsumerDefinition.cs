using MassTransit;

namespace Orchestrun.Core.Hosting.TestConsole;

public sealed class TestConsumerDefinition : ConsumerDefinition<TestConsumer>
{
    public TestConsumerDefinition()
    {
        // Make this one temporary for testing, so the queue gets autodeleted on consumer exit
        Endpoint(e =>
        {
            e.Temporary = true;
        });
    }
}