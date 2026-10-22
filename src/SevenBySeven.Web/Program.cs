using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SevenBySeven.Modules.Collection.Contracts;
using SevenBySeven.Modules.Gigs.Contracts;
using SevenBySeven.Shared.Persistence;
using SevenBySeven.Web;
using SevenBySeven.Web.Api;
using SevenBySeven.Web.Components;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Scanning still runs in this process and calls the modules it needs directly, so they and
// the database are composed here until the Stack moves into the API (docs/adr/0010).
foreach (var module in ModuleCatalog.All)
{
    builder.Services.AddSingleton(module);
    module.RegisterServices(builder.Services, builder.Configuration);
}

builder.AddNpgsqlDbContext<SevenBySevenDbContext>(
    connectionName: "sevenbyseven",
    configureDbContextOptions: options => options.UseSnakeCaseNamingConvention());

// Every other page reaches the modules through the API (docs/adr/0009).
builder.Services.AddApiClients();

// The forms check what they would send against the rules the API enforces.
builder.Services.AddSingleton<IValidator<CopyDetails>, CopyDetailsValidator>();
builder.Services.AddSingleton<IValidator<GigDetails>, GigDetailsValidator>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

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
