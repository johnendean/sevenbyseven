# A Stack lives in the API, not in the browser's connection

A Stack used to live in the Blazor circuit, and that was its entire storage policy: closing the tab ended the Stack, and its photographs went with it. With the modules behind an API (docs/adr/0009), the Stack moves into the API. It is held in memory and never written to the database. There is at most one in progress across the whole application, a Stack left untouched for long enough is abandoned with every Scan it still holds, and the API restarting loses it. Identification runs in the background inside the API, and progress is pushed to whoever is watching over Server-Sent Events.

The reason is that a Stack is the one piece of work long enough to outlast a single screen. Held in the API, it can be captured on a phone at the shelves and worked through at a desk, and a future client gets Stacks without rebuilding them. A Scan that stands alone is different. It lasts one page and is meant to vanish when I leave it, so it stays with the client, and the API only identifies a photograph and answers.

This is written down because each part of it looks like a mistake on its own. A Stack held only in memory looks like a table somebody forgot to add. A single Stack for the whole application looks like a missing id. And a standalone Scan the API knows nothing about looks inconsistent next to a Stack it holds in full. All three are deliberate.

## Considered Options

**Keeping the Stack in Web's circuit,** with the API stateless. This keeps the old policy word for word and needs no expiry. But a Stack could never move between devices, and every future client would have to rebuild Stacks from scratch.

**Persisting Stacks in Postgres.** A Stack would survive a restart, but it would be kept, which is exactly what a Stack and its Scans are defined not to be. It would also write tens of megabytes of photographs that are thrown away minutes later, and it would need cleanup for the ones nobody finishes.

**Many concurrent Stacks, each addressed by its id.** There is one user and one Collection, and a second Stack in progress is almost always a forgotten first one. One Stack at a time means the header in every tab and on every device shows the same pile, and starting a new one while the old one is unresolved is refused until the old one is discarded.

**A standalone Scan as a resource too,** as a Stack of one. It would need the same expiry and cleanup for something whose whole life is a single page.

## Consequences

- The API has to run as a single instance. A second instance would hold a different Stack, or none, and a client would see whichever it reached. Scaling out would first mean moving Stacks somewhere shared, which brings back the question of keeping them.
- Restarting the API abandons the Stack in progress. The README says so, as it used to say that closing the tab did.
- Memory is bounded by the Stack's size limit, which the API clamps. Photographs are held only until each Scan is confirmed or abandoned.
- Adding to a full Stack is an expected outcome now, answered with a 409, and no longer a thrown bug. Two devices can race for the last place, so neither caller is wrong. This narrows the list of bugs in ADR 0008.
- Confirming a Scan in a Stack is one call to the API, which ensures the Release and adds the Copy using the Stack's defaults. The Stack, not the client, knows where the records were bought and where they are kept.
- The Format a Stack starts with is still remembered by each browser, not by the API. It is a convenience for each device and not a fact about the Collection, and memory held in the API would be lost on restart anyway.
