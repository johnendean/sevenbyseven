using SevenBySeven.Api.Persistence;
using SevenBySeven.Modules.Collection.Domain;
using SevenBySeven.Modules.Gigs.Domain;

namespace SevenBySeven.Tests.Api;

public class DesignTimeDbContextFactoryTests
{
    [Fact]
    public void The_migrations_tooling_sees_every_module_s_tables()
    {
        // Generating a migration never opens a connection, so neither does this.
        using var context = new DesignTimeDbContextFactory().CreateDbContext([]);

        Assert.NotNull(context.Model.FindEntityType(typeof(Copy)));
        Assert.NotNull(context.Model.FindEntityType(typeof(Gig)));
    }
}
