using Microsoft.Extensions.Configuration;
using Testcontainers.RabbitMq;

namespace Orchestrun.Core.Hosting.IntegrationTests.Fixtures;

public sealed class RabbitMqFixture : IAsyncLifetime
{
    private RabbitMqContainer? _container;
    public Uri? ConnectionString { get; private set; }
    public async Task InitializeAsync()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var image = config["TestContainers:RabbitMq:Image"]
                    ?? throw new InvalidOperationException("RabbitMQ image not configured in appsettings.json");

        _container = new RabbitMqBuilder(image)
            .WithUsername("admin")
            .WithPassword("password")
            .Build();

        await _container.StartAsync();
        ConnectionString = new Uri(_container.GetConnectionString());
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
            await _container.DisposeAsync();
    }
}
