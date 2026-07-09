using MassTransit;
using Microsoft.Extensions.Hosting;

namespace Orchestrun.Core.Hosting.TestConsole;

public sealed class TestPublisher(IPublishEndpoint endpoint) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var message = $"Hello World! - {DateTime.Now:hh:mm:ss}";
            var testEvent = new TestEvent
            {
                Message = message
            };

            await endpoint.Publish(testEvent, stoppingToken);
            await Task.Delay(TimeSpan.FromMilliseconds(250), stoppingToken);
        }
    }
}