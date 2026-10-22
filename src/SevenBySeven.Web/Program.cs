using FluentValidation;
using SevenBySeven.Modules.Collection.Contracts;
using SevenBySeven.Modules.Gigs.Contracts;
using SevenBySeven.Web.Api;
using SevenBySeven.Web.Components;
using SevenBySeven.Web.Scanning;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Every page reaches the modules through the API (docs/adr/0009). This app holds no
// database connection and no credentials of its own.
builder.Services.AddApiClients();

// The forms check what they would send against the rules the API enforces.
builder.Services.AddSingleton<IValidator<CopyDetails>, CopyDetailsValidator>();
builder.Services.AddSingleton<IValidator<GigDetails>, GigDetailsValidator>();

// A Scan that stands alone lives in the circuit; the Stack lives in the API, and each
// circuit follows it there (docs/adr/0010). The detector's thresholds are the browser's.
builder.Services.AddScoped<ScanSession>();
builder.Services.AddScoped<StackWatch>();
builder.Services.Configure<DetectionOptions>(builder.Configuration.GetSection(DetectionOptions.SectionName));

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
    .AddInteractiveServerRenderMode();

app.Run();
