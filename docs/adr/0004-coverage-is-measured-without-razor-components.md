# Coverage is measured without Razor components

CI fails a branch whose line coverage is below 80%, and Razor components are excluded from the figure that threshold is applied to.

The exclusion is what makes the threshold reachable, so it has to be stated rather than discovered. When the gate was introduced the four pages — `ScanPage`, `CopyPage`, `AddCopyPage` and `CollectionPage` — were 470 of the 843 uncovered lines. With them counted, covering every remaining line of C# in the solution reaches 67.5%; 80% was arithmetically impossible without bUnit tests that drive a camera through `IJSObjectReference`. With them excluded the same suite sat at 61.7%, and the tests that followed took it to 90.6%.

The reason to exclude them is not the arithmetic, though. Coverlet instruments the C# the Razor compiler generates, which is largely the render tree, so a component's percentage measures how much markup a test rendered rather than whether anything behaves correctly — a page can be driven to 100% while every decision it makes is wrong. The same argument already keeps EF's migrations and the Aspire AppHost out of the figure.

This is written down because a future reader will otherwise reasonably read the exclusion as the gate being gamed to hit a number.

## Considered Options

**Count the components and test them with bUnit.** The honest maximum, and the only option that covers the pages at all. It needs 554 more covered lines including `ScanPage`'s camera interop, and most of the resulting assertions would be about markup rather than behaviour.

**Exclude only `ScanPage`,** the one page with JS interop, keeping the three plain-DI pages. Leaves 80% needing 357 of the 373 remaining lines — near-perfect coverage of DI registration and the ZXing luminance adapter. The gate would sit permanently one refactor away from red, which makes it a nuisance rather than a signal.

**A ratchet** that rises to whatever a branch achieved, instead of a fixed floor. Needs committed state and turns unrelated pull requests into coverage negotiations. Coveralls already comments the delta on each pull request, so the signal exists without the gate enforcing it.

## Consequences

- Logic worth testing is lifted out of a page into the module that owns it, which is what `Scan.FromJpeg` already does deliberately. A page that grows a decision worth asserting on is a page with a class missing from behind it.
- `SevenBySeven.Web` and `SevenBySeven.ServiceDefaults` are excluded by name for the same reason the AppHost is. They were already absent from the report because no test project referenced them; naming them means a future `ProjectReference` cannot move the threshold by accident.
- The `IModule.RegisterServices` implementations stay in the denominator, uncovered. They are ordinary application code, and carrying them as a visible cost of the module design is more honest than defining them away — the threshold is met without them.
- The gate reads line coverage only. Branch coverage is reported but not enforced, so there is one number to satisfy rather than two.
