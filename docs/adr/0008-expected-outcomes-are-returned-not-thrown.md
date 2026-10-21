# Expected outcomes are returned, not thrown

An outcome the code expects to happen comes back as a value. A failure that means the code itself is wrong is thrown. In practice that gives four rules:

- **Something that may not be there** comes back as `null` or `false`. `FindAsync` returns `null` for a Copy or Gig that is gone, `UpdateAsync` and `RemoveAsync` return `false` when there was nothing to change, and `TryRead` returns `null` when a photograph has no barcode.
- **An outside service failing** is caught where it is called and turned into a value that says it failed. `DiscogsCatalogue` turns network errors, refusals and a missing token into `DiscogsSearch.Failure`, and a failed fetch into `null`. The value must keep a failure apart from an answer: a search that could not be made is not a search that matched nothing.
- **A bug or a broken rule** is thrown. That covers the guard clauses, a full Stack being added to, a capture that is not a JPEG, and a Gig form read before it has validated. Nothing catches these to carry on; they mean the caller is wrong.
- **Pages catch `Exception`** as the last line of defence, logging it and showing a message rather than a broken circuit. It is a safety net, not the plan: anything a page expects to happen should already have come back as a value.

There used to be one deliberate exception. When Claude could not read a sleeve, `ClaudeSleeveReader` threw `SleeveReadException`, and the Scan and Stack pages caught it by type to show its message. The message carries the API's own words, which say what to change, and a failure dressed up as an empty reading would look exactly like an illegible photograph. This ADR said the exception was the first thing to revisit if identification gained a second caller, and the API is that caller (docs/adr/0009): an exception cannot cross HTTP and keep its meaning. The reader still throws, but only inside the module. `SleeveIdentifier` catches it where it calls the reader and returns the message as `IdentificationResult.ReadFailure`, which keeps it apart from a sleeve that was read and found illegible, just as `DiscogsSearch` keeps a failed search apart from one that matched nothing.

This is written down because a reader will find no `Result<T>` here, and will find `DiscogsSearch` returning a failure where a `Result<T>` might be expected. Without the rule above, that can look like an oversight, and new code would pick a side at random.

## Considered Options

**A general `Result<T>`,** hand-written or from a library such as FluentResults or ErrorOr. Every call site would unwrap a wrapper to learn what `null` or `false` already says. It would also replace names that carry meaning, such as `MatchedNothing`, `IsOutsideFormat` and `SearchFailed`, with a generic success-or-error that the reader has to interpret.

**Exceptions for everything,** so a missing Copy or an unreachable Discogs throws. A Copy deleted in another tab and Discogs being down are both ordinary, and identification is meant to degrade rather than stop when Discogs or its token is missing. Every caller would then need a try block to carry on, which is a value with extra steps.

## Consequences

- An empty list, a `null` or a `false` must not mean two things. `DiscogsSearch` exists because an empty list once meant both "no matches" and "could not search", and the Format fallback treated a failure as an answer.
- A new outside service follows Discogs: catch at the boundary, return a value that keeps failure apart from an answer, and let the page say which happened.
- What I type is validated before anything is saved, so the exceptions thrown below that only ever mean a bug. The forms check it first, so a page can say what is wrong beside the field. The API checks it again, because it is the boundary every client crosses (docs/adr/0009), and answers a 400 that names each field instead of letting the database refuse it as a 500.
