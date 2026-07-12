using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.FileProviders;
using Orchestrun.Core.Contracts.Events;
using Orchestrun.Core.Hosting.Consumers;
using Orchestrun.Core.Hosting.IntegrationTests.Fixtures;
using Orchestrun.Core.Hosting.Services;

namespace Orchestrun.Core.Hosting.IntegrationTests;

[Collection("Integration Collection")]

public sealed class ServiceSettingsChangedConsumerTests(RabbitMqFixture fixture) : TestBase
{
    [Fact]
    public async Task Consumer_should_consume_service_settings_changed_message()
    {
        await using var provider = new ServiceCollection()
            .AddSingleton<IHostEnvironment>(new TestHostEnvironment("TestService"))
            .AddSingleton<IServiceSettingsProvider, ServiceSettingsProvider>()
            .AddSingleton<IServiceSettingsWriter, ServiceSettingsProvider>()
            .AddMassTransitTestHarness(cfg =>
            {
                cfg.AddConsumer<ServiceSettingsChangedConsumer>();

                cfg.UsingRabbitMq((context, host) =>
                {
                    host.Host(fixture.ConnectionString);
                    host.ConfigureEndpoints(context);
                });
            })
            .BuildServiceProvider();

        var harness = provider.GetTestHarness();
        await harness.Start();
        
        var message = new ServiceSettingsChanged
        {
            TargetServiceName = "TestService",
            IsGlobal = false,
            Key = "TestKey",
            Value = "TestValue",
        };
        
        await harness.Bus.Publish(message);
        
        var consumerHarness = harness.GetConsumerHarness<ServiceSettingsChangedConsumer>(); 
        Assert.True(await consumerHarness.Consumed.Any<ServiceSettingsChanged>());

        var settingsEvent = await consumerHarness.Consumed.SelectAsync<ServiceSettingsChanged>().FirstOrDefault();
        Assert.NotNull(settingsEvent);
        Assert.Equal("TestKey", settingsEvent.Context.Message.Key);
        Assert.Equal("TestValue", settingsEvent.Context.Message.Value);
        Assert.Equal(message.TargetServiceName, settingsEvent.Context.Message.TargetServiceName);
        Assert.False(settingsEvent.Context.Message.IsGlobal);
        
        await harness.Stop();
    }

    private sealed class TestHostEnvironment(string applicationName) : IHostEnvironment
    {
        public string ApplicationName { get; set; } = applicationName;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public string EnvironmentName { get; set; } = Environments.Development;
    }
}