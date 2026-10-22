var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgWeb();

var database = postgres.AddDatabase("sevenbyseven");

// The API owns the database and applies its migrations on boot. It has no external
// endpoint: only the web app reaches it (docs/adr/0009).
var api = builder.AddProject<Projects.SevenBySeven_Api>("api")
    .WithReference(database)
    .WaitFor(database);

// Waits for the API so that the migrations have run before anything here touches the
// database, which Scanning still does in-process until the Stack moves into the API.
builder.AddProject<Projects.SevenBySeven_Web>("web")
    .WithReference(database)
    .WithReference(api)
    .WaitFor(api)
    .WithExternalHttpEndpoints();

builder.Build().Run();
