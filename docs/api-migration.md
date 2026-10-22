# Moving the modules behind an API

A handoff note for the work that moved the modules out of the Blazor app and behind an
HTTP API. It records where the work stands, not why it was done that way. The reasons are
in the ADRs:

- [0008](adr/0008-expected-outcomes-are-returned-not-thrown.md): how expected outcomes are returned, and how they cross HTTP
- [0009](adr/0009-modules-are-a-headless-api-in-their-own-process.md): modules are a headless API in their own process
- [0010](adr/0010-a-stack-lives-in-the-api.md): a Stack lives in the API

Delete this file once the last step has merged.

## Done

| Step | PR |
| --- | --- |
| ADRs 0009 and 0010; the Scan and Stack definitions in CONTEXT.md | #24 |
| Contracts projects, module endpoints and the `SevenBySeven.Api` host; `SleeveReadException` returned as a value | #25 |
| API request validation moved to FluentValidation, with the validators next to the contracts | #26 |
| Collection and Gigs pages moved into Web and onto typed HTTP clients; Aspire runs `api` and `web` side by side | #27 |
| The Stack held in the API with Server-Sent Events; Scan and Stack pages moved into Web; Web references only the contracts | #28 |

Web now has no module references, no database connection and no credentials. Everything
goes through the API.

## Still to do: PR 4, docs only

- **README "Layout":** it still says Web composes the modules and that Collection carries
  pages. Add `SevenBySeven.Api` and the `*.Contracts` projects, and say that every module is
  composed into the API while Web holds all the pages.
- **README module paragraph** below that table: modules no longer carry their own pages.
- **README configuration:**
  - `Scanning__MaxStackSize` and `Scanning__StackIdleTimeout` are read by the API.
  - `Scanning__Detection__*` is read by Web.
  - Check that setting them as an environment variable on `dotnet run --project src/SevenBySeven.AppHost` actually reaches the project that reads them. Aspire may not pass the parent's environment through.
- **README "Tests" and ADR 0004:** the API host is counted towards coverage, and so is
  generated source under `obj/`, which the coverage settings now exclude. Check that both
  still read correctly.
- **A last pass over ADRs 0009 and 0010** against the code.
- **Delete this file.**

## Untested by hand

The tests cover the API calls behind these, but nobody has tried them in a browser:

- capturing a Stack with the live camera and the detector
- reviewing and confirming a Stack, including the Stack's defaults
- picking a Stack up on a second device, and the header marker showing it there
- a real photograph going through Claude and Discogs, both as a single Scan and in a Stack
- form validation messages appearing beside the fields on the Copy and Gig forms
- a Stack being abandoned by the idle timeout

## Loose ends

- `StackPage` keeps a per-tab cache of photographs. It's cleared on Begin and Discard, but
  not when the Stack is abandoned for being idle or is thrown away on another device, so an
  open tab holds those photographs until it closes.
- `StackWatch`'s one-read-at-a-time fix has no test of its own.
