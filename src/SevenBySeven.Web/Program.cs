using Microsoft.EntityFrameworkCore;
using SevenBySeven.Shared.Modularity;
using SevenBySeven.Shared.Persistence;
using SevenBySeven.Web;
using SevenBySeven.Web.Components;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

foreach (var module in ModuleCatalog.All)
{
    builder.Services.AddSingleton(module);
    module.RegisterServices(builder.Services, builder.Configuration);
}

builder.AddNpgsqlDbContext<SevenBySevenDbContext>(
    connectionName: "sevenbyseven",
    configureDbContextOptions: options => options.UseSnakeCaseNamingConvention());

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Dev convenience: bring the database up to date on boot. Production would apply
// migrations deliberately rather than as a side effect of starting the app.
if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    var database = scope.ServiceProvider.GetRequiredService<SevenBySevenDbContext>();
    await database.Database.MigrateAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapDefaultEndpoints();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    // Server-side endpoint discovery is separate from the Router's AdditionalAssemblies:
    // without this, a module's pages compile and link but 404.
    .AddAdditionalAssemblies([.. ModuleCatalog.RoutableAssemblies]);

app.Run();
