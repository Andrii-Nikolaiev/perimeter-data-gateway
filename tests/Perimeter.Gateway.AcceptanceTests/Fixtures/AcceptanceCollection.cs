namespace Perimeter.Gateway.AcceptanceTests.Fixtures;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class AcceptanceCollection
    : ICollectionFixture<AcceptanceEnvironment>
{
    public const string Name = "Acceptance";
}
