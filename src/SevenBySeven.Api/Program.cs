using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using SevenBySeven.Shared.Modularity;
using SevenBySeven.Shared.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var modules = ModuleDiscovery.FindIn(typeof(Program).Assembly);

foreach (var module in modules)
{
    builder.Services.AddSingleton(module);
    module.RegisterServices(builder.Services, builder.Configuration);
}

builder.AddNpgsqlDbContext<SevenBySevenDbContext>(
    connectionName: "sevenbyseven",
    configureDbContextOptions: options => options.UseSnakeCaseNamingConvention());

// Enums go over the wire by name: "VeryGoodPlus" means something to a client that was not
// compiled against this code, and 6 does not.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var app = builder.Build();

// Dev convenience: bring the database up to date on boot. Production would apply
// migrations deliberately rather than as a side effect of starting the app.
if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    var database = scope.ServiceProvider.GetRequiredService<SevenBySevenDbContext>();
    await database.Database.MigrateAsync();
}

// A bug comes back as a 500 with a ProblemDetails body, and a bare status such as a 404
// gets one too, so every failure a client sees has the same shape (docs/adr/0009). A body
// that cannot be read is the client's fault, not ours: in Development the framework throws
// it rather than answering, and it must still come back as the 400 it is.
app.UseExceptionHandler(new ExceptionHandlerOptions
{
    StatusCodeSelector = exception => exception is BadHttpRequestException bad
        ? bad.StatusCode
        : StatusCodes.Status500InternalServerError,
});
app.UseStatusCodePages();

app.MapDefaultEndpoints();

// The document a client that is not this solution's Web project would build against.
app.MapOpenApi();

// A page for reading that document and trying the endpoints by hand, at /scalar. Development
// only, like the health checks: the API has no authentication (docs/adr/0009).
if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference();
}

foreach (var module in modules)
{
    module.MapEndpoints(app);
}

app.Run();
