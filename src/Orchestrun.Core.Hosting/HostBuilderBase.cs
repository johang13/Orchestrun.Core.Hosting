using System.Reflection;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Orchestrun.Core.Hosting.Options;
using Orchestrun.Core.Hosting.Services;

namespace Orchestrun.Core.Hosting;

public abstract class HostBuilderBase<TBuilder>(
    IHostApplicationBuilder builder)
    where TBuilder : HostBuilderBase<TBuilder>
{
    private readonly List<Action<IRabbitMqBusFactoryConfigurator>> _rabbitMqConfigurations = [];

    protected void BuildCore()
    {
        AddAppSettings();
        RegisterDefaults();
        RegisterBus();
    }

    public TBuilder ConfigureServices(Action<IConfiguration, IServiceCollection, IHostEnvironment> configureServices)
    {
        configureServices.Invoke(builder.Configuration, builder.Services, builder.Environment);
        return (TBuilder)this;
    }

    public TBuilder ConfigureRabbitMq(Action<IRabbitMqBusFactoryConfigurator> configureRabbitMq)
    {
        _rabbitMqConfigurations.Add(configureRabbitMq);
        return (TBuilder)this;
    }

    private void RegisterDefaults()
    {
        builder.Services.AddMemoryCache();
        builder.Services.AddHealthChecks();
        builder.Services.AddProblemDetails();
        builder.Services.AddOpenApi();
        builder.Services.AddEndpointsApiExplorer();
        
        builder.Services.AddHostedService<NugetDependencyPublisher>();
    }

    private void AddAppSettings()
    {
        builder.Configuration.AddJsonFile("appsettings.json", true, true);
        if (builder.Environment.IsEnvironment("Local"))
            builder.Configuration.AddJsonFile("appsettings.Local.json", true, true);
        else if (Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true")
            builder.Configuration.AddJsonFile("appsettings.Docker.json", true, true);
    }

    private void RegisterBus()
    {
        builder.Services.AddOptions<RabbitMqOptions>()
            .Bind(builder.Configuration.GetSection(RabbitMqOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        
        builder.Services.AddMassTransit(x =>
        {
            x.AddConsumers(Assembly.GetEntryAssembly());
            x.AddConfigureEndpointsCallback((context, name, cfg) => { cfg.UseInMemoryOutbox(context); });
            x.UsingRabbitMq((context, cfg) =>
            {
                var rabbitMqOptions = context.GetRequiredService<IOptions<RabbitMqOptions>>().Value;

                cfg.Host(rabbitMqOptions.Host, rabbitMqOptions.VirtualHost, h =>
                {
                    h.Username(rabbitMqOptions.Username);
                    h.Password(rabbitMqOptions.Password);
                });
                
                cfg.UseMessageRetry(r => r.Exponential(
                    5,
                    TimeSpan.FromMilliseconds(200),
                    TimeSpan.FromSeconds(30),
                    TimeSpan.FromMilliseconds(200)));

                cfg.DeployPublishTopology = true;

                foreach (var configureRabbitMq in _rabbitMqConfigurations)
                    configureRabbitMq(cfg);

                cfg.ConfigureEndpoints(context);
            });
        });
    }
}