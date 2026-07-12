using Microsoft.Extensions.DependencyInjection;
using Orchestrun.Core.Hosting;
using Orchestrun.Core.Hosting.TestConsole;

var host = OrchestrunHost
    .CreateBuilder(args)
    .ConfigureServices((cfg, services, host) =>
    {
        services.AddHostedService<TestPublisher>();
        services.AddHostedService<TestDbWriter>();
        services.AddScoped<TestItemRepository>();
    })
    .ConfigureRabbitMq(cfg =>
    {
        cfg.Publish<TestEvent>(t =>
        {
            t.AutoDelete = true;
            t.Durable = false;
        });
    })
    .AddDatabase("OrchestrunDb", typeof(Program).Assembly)
    .Build();

host.Run();