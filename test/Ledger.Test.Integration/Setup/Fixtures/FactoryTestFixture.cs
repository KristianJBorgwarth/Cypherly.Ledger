using Ledger.Infrastructure.Persistence.Context;

namespace Ledger.Test.Integration.Setup.Fixtures;

[CollectionDefinition("LedgerApplication")]
public class FactoryTestFixture : ICollectionFixture<IntegrationTestFactory<Program, LedgerDbContext>>
{

}
