namespace SevenBySeven.Shared.Persistence;

/// <summary>
/// Postgres schema per module. The database is shared, but each module's tables
/// live in its own schema so the module boundary is visible in the database too.
/// </summary>
public static class Schemas
{
    public const string Catalogue = "catalogue";
    public const string Collection = "collection";
}
