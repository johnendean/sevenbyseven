using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SevenBySeven.Shared.Modularity;

namespace SevenBySeven.Modules.Scanning;

/// <summary>
/// Owns the camera capture UI and photograph intake. Holds nothing durable:
/// photographs are discarded once a Scan ends.
/// </summary>
public sealed class ScanningModule : IModule
{
    public string Name => "Scanning";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration) =>
        services.AddScoped<ScanSession>();

    public void ConfigureModel(ModelBuilder modelBuilder)
    {
    }
}
