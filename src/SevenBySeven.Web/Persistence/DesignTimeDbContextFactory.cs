using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SevenBySeven.Shared.Persistence;

namespace SevenBySeven.Web.Persistence;

/// <summary>
/// Used only by the EF Core tooling when adding or scripting migrations. The
/// connection string is a placeholder: the real one comes from Aspire at run time,
/// and generating a migration never opens a connection.
/// </summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<SevenBySevenDbContext>
{
    public SevenBySevenDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<SevenBySevenDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=sevenbyseven;Username=postgres;Password=postgres")
            .UseSnakeCaseNamingConvention()
            .Options;

        return new SevenBySevenDbContext(options, ModuleCatalog.All);
    }
}
