using System.Reflection;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyModel;
using Orchestrun.Core.Contracts.Events;
using Orchestrun.Core.Hosting.IntegrationTests.Fixtures;
using Orchestrun.Core.Hosting.Services;

namespace Orchestrun.Core.Hosting.IntegrationTests;

[Collection("Integration Collection")]
public sealed class NuGetDependencyPublisherTests(RabbitMqFixture fixture) : TestBase
{
    [Fact]
    public async Task Dependencies_Should_Be_Published()
    {
        await using var provider = new ServiceCollection()
            .AddHostedService<NugetDependencyPublisher>()
            .AddMassTransitTestHarness(cfg =>
            {
                cfg.UsingRabbitMq((context, host) =>
                {
                    host.Host(fixture.ConnectionString);
                });
            })
            .BuildServiceProvider();
        
        var harness = provider.GetTestHarness();
        
        var context = DependencyContext.Default
                      ?? throw new InvalidOperationException(
                          "No DependencyContext available. Make sure a *.deps.json file is published next to the entry assembly.");
        
        await harness.Start();
        
        var publishedEvent = await harness.Published.SelectAsync<NuGetDependenciesPublished>().FirstOrDefault();
        
        Assert.True(publishedEvent is not null, "No NuGetDependenciesPublished event was published");
        Assert.Equal(publishedEvent.Context.Message.ServiceName, Assembly.GetEntryAssembly()?.GetName().Name);
        Assert.NotEmpty(publishedEvent.Context.Message.Dependencies);
        await harness.Stop();
    }
}