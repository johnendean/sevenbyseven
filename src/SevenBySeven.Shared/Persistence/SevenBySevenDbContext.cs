using Microsoft.EntityFrameworkCore;
using SevenBySeven.Shared.Modularity;

namespace SevenBySeven.Shared.Persistence;

/// <summary>
/// The single context over the application database. Modules do not own their own
/// context: a Copy holds a real foreign key to its Release, and
/// enforcing that in the database is worth more here than per-module isolation.
/// Each module contributes its own entity configuration via <see cref="IModule"/>.
/// </summary>
public sealed class SevenBySevenDbContext(
    DbContextOptions<SevenBySevenDbContext> options,
    IEnumerable<IModule> modules) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        foreach (var module in modules)
        {
            module.ConfigureModel(modelBuilder);
        }
    }
}
