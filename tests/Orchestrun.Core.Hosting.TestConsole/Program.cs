using Microsoft.Extensions.DependencyInjection;
using Orchestrun.Core.Hosting;
using Orchestrun.Core.Hosting.TestConsole;

var host = OrchestrunHost
    .CreateBuilder(args)
    .ConfigureServices((cfg, services, host) =>
    {
        services.AddHostedService<TestPublisher>();
    })
    .ConfigureRabbitMq(cfg =>
    {
        cfg.Publish<TestEvent>(t =>
        {
            t.AutoDelete = true;
            t.Durable = false;
        });
    })
    .Build();

host.Run();