using System.Data.Common;
using System.Reflection;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Orchestrun.Core.Hosting.Consumers;
using Orchestrun.Core.Hosting.Database;
using Orchestrun.Core.Hosting.Options;
using Orchestrun.Core.Hosting.Services;
using Polly;
using Polly.Retry;

namespace Orchestrun.Core.Hosting;

/// <summary>
///     Base class for building and configuring host environments in the Orchestrun framework.
///     Provides a foundation for registering services, configuring RabbitMQ, and adding application settings.
/// </summary>
/// <typeparam name="TBuilder">
///     The specific type of the builder that extends <see cref="HostBuilderBase{TBuilder}" />.
/// </typeparam>
public abstract class HostBuilderBase<TBuilder>(
    IHostApplicationBuilder builder)
    where TBuilder : HostBuilderBase<TBuilder>
{
    private readonly List<PendingMigration> _pendingMigrations = [];
    private readonly List<Action<IRabbitMqBusFactoryConfigurator>> _rabbitMqConfigurations = [];

    /// <summary>
    ///     Configures core components and settings required for the host environment.
    ///     Invokes internal methods to add application settings, register default services,
    ///     and configure MassTransit with RabbitMQ.
    ///     This method is central to preparing the underlying infrastructure before building the host instance.
    /// </summary>
    protected void BuildCore()
    {
        AddAppSettings();
        RegisterDefaults();
        ConfigureOpenTelemetry();
        ConfigureResilience();
        RunPendingMigrations();
        RegisterBus();
    }

    /// <summary>
    ///     Configures the application's service collection by allowing the caller to specify custom service registrations,
    ///     application-level configuration interactions, and environment-specific adjustments.
    ///     This method provides a customizable extension point for setting up the dependency injection container and related
    ///     services.
    /// </summary>
    /// <param name="configureServices">
    ///     A delegate that takes the application's configuration, the service collection,
    ///     and the host environment as parameters for customization of services and settings.
    /// </param>
    /// <returns>
    ///     The current instance of the builder, allowing for method chaining during host construction.
    /// </returns>
    public TBuilder ConfigureServices(Action<IConfiguration, IServiceCollection, IHostEnvironment> configureServices)
    {
        configureServices.Invoke(builder.Configuration, builder.Services, builder.Environment);
        return (TBuilder)this;
    }

    /// <summary>
    ///     Adds a configuration delegate for customizing RabbitMQ during the MassTransit setup.
    ///     This method allows detailed configuration of RabbitMQ features like exchanges, queues, and bindings
    ///     by adding the provided actions to an internal list of RabbitMQ configuration steps.
    ///     The configurations will be applied when MassTransit is initialized during the host build process.
    /// </summary>
    /// <param name="configureRabbitMq">
    ///     An action delegate that enables customization of RabbitMQ settings using the
    ///     <see cref="IRabbitMqBusFactoryConfigurator" />.
    ///     This delegate provides access to RabbitMQ-specific features and is invoked during the MassTransit bus
    ///     configuration.
    /// </param>
    /// <returns>
    ///     The current instance of the builder, enabling method chaining for additional host configurations.
    /// </returns>
    public TBuilder ConfigureRabbitMq(Action<IRabbitMqBusFactoryConfigurator> configureRabbitMq)
    {
        _rabbitMqConfigurations.Add(configureRabbitMq);
        return (TBuilder)this;
    }

    /// <summary>
    ///     Registers a logical Postgres database identified by <paramref name="databaseName" />.
    ///     Reads the connection string from configuration under <c>ConnectionStrings:{databaseName}</c>,
    ///     registers a keyed <see cref="NpgsqlDataSource" /> and a keyed <see cref="ResiliencePipeline" />
    ///     under that name, adds a health check surfaced on <c>/health/ready</c>, and queues pending SQL
    ///     migrations embedded in <paramref name="migrationsAssembly" /> to run during <see cref="BuildCore" />.
    ///     Repositories should be registered separately with the standard DI container (e.g.
    ///     <c>services.AddScoped&lt;TRepository&gt;()</c>), requesting the data source/pipeline via
    ///     constructor parameters attributed with <c>[FromKeyedServices(databaseName)]</c> - the DI
    ///     container resolves this automatically, no custom registration helper is required.
    /// </summary>
    /// <param name="databaseName">
    ///     The logical database name. Used both as the configuration key (<c>ConnectionStrings:{databaseName}</c>)
    ///     and as the keyed-service name repositories should reference via <c>[FromKeyedServices]</c>.
    /// </param>
    /// <param name="migrationsAssembly">The assembly containing the DbUp SQL scripts embedded as resources.</param>
    /// <returns>The current instance of the builder, enabling method chaining.</returns>
    public TBuilder AddDatabase(string databaseName, Assembly migrationsAssembly)
    {
        var connectionString = builder.Configuration.GetConnectionString(databaseName)
                               ?? throw new InvalidOperationException(
                                   $"Missing configuration 'ConnectionStrings:{databaseName}' required for database '{databaseName}'.");

        builder.Services.AddNpgsqlDataSource(connectionString, serviceKey: databaseName);
        builder.Services.AddKeyedSingleton(databaseName, (_, _) => CreateDatabaseResiliencePipeline());
        builder.Services.AddHealthChecks().AddNpgSql(connectionString, name: $"postgres-{databaseName}");

        _pendingMigrations.Add(new PendingMigration(databaseName, connectionString, migrationsAssembly));

        return (TBuilder)this;
    }

    private static ResiliencePipeline CreateDatabaseResiliencePipeline()
    {
        return new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder()
                    .Handle<DbException>()
                    .Handle<TimeoutException>(),
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromMilliseconds(200),
                UseJitter = true
            })
            .AddTimeout(TimeSpan.FromSeconds(10))
            .Build();
    }

    private void RunPendingMigrations()
    {
        if (_pendingMigrations.Count == 0)
            return;

        // Postgres may not be reachable yet at cold start (e.g. compose/orchestrator races),
        // so migrations are retried with backoff before failing the host startup.
        var retryPipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                MaxRetryAttempts = 5,
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromSeconds(1),
                UseJitter = true
            })
            .Build();

        foreach (var migration in _pendingMigrations)
            try
            {
                retryPipeline.Execute(() =>
                    DatabaseMigrator.Run(migration.ConnectionString, migration.MigrationsAssembly));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to migrate database '{migration.DatabaseName}' after retries.", ex);
            }
    }

    private void RegisterDefaults()
    {
        builder.Services.AddMemoryCache();
        builder.Services.AddHealthChecks();
        builder.Services.AddProblemDetails();
        builder.Services.AddOpenApi();
        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddSingleton<IServiceSettingsProvider, ServiceSettingsProvider>();
        builder.Services.AddSingleton<IServiceSettingsWriter, ServiceSettingsProvider>();
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

    private void ConfigureOpenTelemetry()
    {
        var serviceName = builder.Environment.ApplicationName;

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter("MassTransit"))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddSource("MassTransit"));

        // Only export via OTLP if an endpoint is actually configured — otherwise every
        // service would spend startup trying (and failing) to reach a collector that isn't there.
        if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
    }

    private void ConfigureResilience()
    {
        // Database-specific resilience pipelines are registered per logical database (keyed by
        // name) in AddDatabase<TDbKey>, since OrchestrunRepositoryBase resolves a keyed
        // ResiliencePipeline rather than a single shared one.
        builder.Services.ConfigureHttpClientDefaults(http => http.AddStandardResilienceHandler());
    }

    private void RegisterBus()
    {
        builder.Services.AddOptions<RabbitMqOptions>()
            .Bind(builder.Configuration.GetSection(RabbitMqOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddMassTransit(x =>
        {
            x.AddConsumers(typeof(ServiceSettingsChangedConsumer).Assembly);

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

    private readonly record struct PendingMigration(
        string DatabaseName,
        string ConnectionString,
        Assembly MigrationsAssembly);
}