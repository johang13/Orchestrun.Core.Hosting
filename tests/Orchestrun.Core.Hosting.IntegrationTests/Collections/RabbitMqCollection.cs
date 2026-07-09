using Orchestrun.Core.Hosting.IntegrationTests.Fixtures;

namespace Orchestrun.Core.Hosting.IntegrationTests.Collections;

[CollectionDefinition("RabbitMQ collection")]
public class RabbitMqCollection : ICollectionFixture<RabbitMqFixture>
{
}
