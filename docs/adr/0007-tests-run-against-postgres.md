# Tests run against Postgres

The tests that touch the database used to run against SQLite in memory. The reasoning was that `dotnet test` should not need a container, and that a real relational provider would still enforce keys, foreign keys and unique indexes. They now run against Postgres, the same image the AppHost runs, in a container that Testcontainers starts once per run. Each test gets its own database, copied from a template built by the real migrations.

This is written down because a reader who finds Docker needed just to run the tests will reasonably wonder why an in-memory database was not good enough. It was not good enough because SQLite was only an approximation, and the gaps were in exactly the places the tests exist to check. The schema came from `EnsureCreated`, so the migrations the app applies on startup were never exercised. Every `DateTimeOffset` needed a test-only converter before SQLite would order by it. `text[]`, `interval` and schemas were all stood in for or ignored. Postgres-specific behaviour, such as unique indexes being checked row by row, which shapes `SelectionConfiguration`, could not be observed at all. A green run proved that the code worked on SQLite, and nobody runs it on SQLite.

## Considered Options

**Keeping SQLite and adding a Postgres run in CI.** This keeps the fast, container-free default, but means maintaining two providers and their quirks. It also leaves "passes locally" meaning less than "passes in CI".

**Aspire's testing host.** It already knows how to start Postgres, but it brings up the whole AppHost (pgweb and the web project too) for tests that only want a database.

**A Postgres service container in CI.** This only works in CI, or against a Postgres you happen to run yourself, so local runs would differ from CI.

## Consequences

- `dotnet test` needs a running container runtime. Without one, every database test fails with Testcontainers' "Docker is either not running or misconfigured" rather than skipping, so the coverage gate is never red for a reason you can't see.
- The first run on a machine pulls the image. After that, starting the container and migrating the template adds a few seconds per run, and each test's own database takes milliseconds to copy.
- The image tag in `TestDatabase` follows the AppHost's Aspire version and has to be moved when Aspire's is.
