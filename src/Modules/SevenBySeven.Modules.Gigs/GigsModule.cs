using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SevenBySeven.Modules.Collection;
using SevenBySeven.Shared.Modularity;

namespace SevenBySeven.Modules.Gigs;

/// <summary>
/// Owns the Gigs I have played and what I played at each, and judges Repeats from them.
/// Reads the Collection; never writes to it. Supplies the Collection's
/// <see cref="IPlayHistory"/> in return.
/// </summary>
public sealed class GigsModule : IModule
{
    public string Name => "Gigs";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<GigsOptions>(configuration.GetSection(GigsOptions.SectionName));

        services.AddScoped<IGigLog, GigLog>();
        services.AddScoped<IPlayHistory, PlayHistory>();
    }

    public void ConfigureModel(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GigsModule).Assembly);
}
