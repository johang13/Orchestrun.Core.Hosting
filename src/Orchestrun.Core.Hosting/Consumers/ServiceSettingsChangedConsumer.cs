using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orchestrun.Core.Contracts.Events;
using Orchestrun.Core.Hosting.Services;

namespace Orchestrun.Core.Hosting.Consumers;

internal sealed class ServiceSettingsChangedConsumer(
    IServiceSettingsWriter serviceSettingsProvider,
    ILogger<ServiceSettingsChangedConsumer> logger,
    IHostEnvironment hostEnvironment) : IConsumer<ServiceSettingsChanged>
{
    private readonly string _serviceName = hostEnvironment.ApplicationName;

    public Task Consume(ConsumeContext<ServiceSettingsChanged> context)
    {
        var message = context.Message; 
        logger.LogDebug("Received service settings changed for service '{ServiceName}', key '{Key}', value '{Value}'",
            message.TargetServiceName,
            message.Key,
            message.Value);
        
        // discard events that aren't global settings or settings for this service
        if (message.TargetServiceName == _serviceName || message.IsGlobal)
            serviceSettingsProvider.Set(message.Key, message.Value, message.IsGlobal);

        return Task.CompletedTask;
    }
}