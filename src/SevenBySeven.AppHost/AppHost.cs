var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgWeb();

var database = postgres.AddDatabase("sevenbyseven");

// The API owns the database and applies its migrations on boot. It has no external
// endpoint: only the web app reaches it (docs/adr/0009).
//
// Healthy, not merely running, is what the web app waits for. The API migrates before it
// starts listening, so /health answers only once the schema is current and the database
// is reachable. The endpoint is mapped in Development only, as is migrating on boot, and
// Development is the only environment this AppHost runs in.
var api = builder.AddProject<Projects.SevenBySeven_Api>("api")
    .WithReference(database)
    .WaitFor(database)
    .WithHttpHealthCheck("/health")
    .WithUrlForEndpoint("http", _ => new() { Url = "/scalar", DisplayText = "API reference" });

// The web app reaches everything through the API and holds no database connection of
// its own. It waits for the API to be healthy so its first page has something to read.
builder.AddProject<Projects.SevenBySeven_Web>("web")
    .WithReference(api)
    .WaitFor(api)
    .WithExternalHttpEndpoints();

builder.Build().Run();
