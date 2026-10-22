using FluentValidation;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SevenBySeven.Modules.Scanning.Contracts;
using SevenBySeven.Shared.Modularity;

namespace SevenBySeven.Modules.Scanning;

/// <summary>
/// Owns the Stack: the one in progress, its photographs while they wait, and identifying
/// them behind the capture (docs/adr/0010). Holds nothing durable: a photograph is let go
/// once its Scan ends, and a Stack nobody comes back to is abandoned.
/// </summary>
public sealed class ScanningModule : IModule
{
    public string Name => "Scanning";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ScanningOptions>(configuration.GetSection(ScanningOptions.SectionName));

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<StackHolder>();
        services.AddHostedService<StackIdentification>();
        services.AddHostedService<StackSweep>();

        services.AddSingleton<IValidator<StackDefaults>, StackDefaultsValidator>();
    }

    public void ConfigureModel(ModelBuilder modelBuilder)
    {
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) =>
        ScanningEndpoints.Map(endpoints);
}
