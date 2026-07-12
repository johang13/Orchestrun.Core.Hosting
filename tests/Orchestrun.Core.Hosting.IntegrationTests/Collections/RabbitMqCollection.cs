using Orchestrun.Core.Hosting.IntegrationTests.Fixtures;

namespace Orchestrun.Core.Hosting.IntegrationTests.Collections;

[CollectionDefinition("Integration Collection")]
public class RabbitMqCollection : ICollectionFixture<RabbitMqFixture>, ICollectionFixture<PostgresFixture>
{
}
