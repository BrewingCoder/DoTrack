namespace DoTrack.Infrastructure.Tests.Fixtures;

[CollectionDefinition(nameof(PostgresCollection))]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>;

[CollectionDefinition(nameof(SqlServerCollection))]
public sealed class SqlServerCollection : ICollectionFixture<SqlServerFixture>;

[CollectionDefinition(nameof(SqliteCollection))]
public sealed class SqliteCollection : ICollectionFixture<SqliteFixture>;
