namespace DoTrack.Integration.Tests;

[CollectionDefinition(nameof(IntegrationCollection))]
public sealed class IntegrationCollection : ICollectionFixture<DoTrackApiFactory>;
