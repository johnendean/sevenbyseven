# Modules are a headless API in their own process

Until now each module has carried its own pages, and those pages have injected the module's services directly into a Blazor Server circuit. The modules now run in a process of their own, `SevenBySeven.Api`, which owns the database, the migrations and the Discogs and Anthropic credentials, and exposes each module over HTTP. `SevenBySeven.Web` holds every page and reaches the modules only through that API, using each module's `Contracts` project and a typed HTTP client. Web references no module project at all.

The point is a boundary that cannot be crossed by accident. While a page could inject `IVinylCollection`, nothing stopped UI code from reaching past a module's intended surface, and nothing showed whether the modules could serve anything other than these pages. With Web on the far side of a network hop, the API is complete because the only client there is has nothing else to call. That keeps open the second client `docs/mobile-distribution.md` circles around, without designing one now.

This is written down because a single-user catalogue in which one Blazor app makes HTTP calls to a sibling process on the same machine looks like over-engineering, and a reader could reasonably fold the two back together to save the hop. The hop is what enforces the boundary, and that is the reason for the split.

## Considered Options

**Endpoints alongside the pages,** with both calling the module services in-process. Cheapest, and Blazor Server pays nothing for serialisation it does not need. But nothing proves the API is complete. It drifts behind the pages as soon as a page can do something the API cannot, and nothing would notice until a second client found the gap.

**One process, with Web calling the API over loopback.** It keeps one deployable, but the host still references every module, so a page can still inject a service in place of the client, and the credentials and database stay in the UI host. The boundary would be a convention again.

**A UI project per module** (`Modules.X.UI`), which keeps vertical slices: a module's pages arrive with it. Rejected in favour of one UI app. Once pages talk to contracts and not to services, nothing is left to keep a page next to the module it shows. A future client will draw its own screens anyway.

**Clients generated from OpenAPI** in place of shared C# contracts. That suits a non-.NET client, so the API publishes its OpenAPI document. For Web, though, hand-written clients over per-module contract records keep the boundaries the compiler already enforces, and there is no generator to run.

**Blazor WebAssembly,** which needs an HTTP boundary anyway. It was not the goal. Web stays on InteractiveServer, and its circuit now holds only UI state.

## Consequences

- The API has no authentication, and Aspire gives it no external endpoint: only Web can reach it. This holds only while Web is the only client. The day a second client arrives, real user authentication is the first thing to decide, before any new endpoint.
- The API returns contract records, never EF entities. `Copy`, `Release` and the rest stay inside their modules, and a change to an entity does not change the API unless someone changes a contract.
- A module's UI no longer arrives with the module. Discovery moves to the API host and only finds services, entity configuration and endpoints.
- Expected outcomes cross the wire as ADR 0008 maps them: a missing resource is a 404, a domain outcome such as `MatchedNothing` or `SearchFailed` is a 200 that says so, and a bug is a 500. Web's pages keep their catch-all as a safety net.
- The API host has behaviour of its own now, so it counts towards coverage and its endpoints are tested against Postgres. Web stays out of the figure, and its HTTP clients are kept thin enough that leaving it out is honest.
- Every page action is now an extra round trip inside the deployment. For one user that cost is not measurable, and the benefit is the boundary itself.
