# Getting this onto a phone

Nothing here is decided. This is the shape of the problem as it stood in September 2026,
written down so the same ground does not have to be covered again from scratch.

## What the architecture allows

The app is Blazor Server. Every interaction is a round trip over a WebSocket to a host
that owns the database and holds the Discogs and Anthropic credentials. Nothing worth
calling an app can run without that host being reachable, so anything resembling offline
capture is a rewrite rather than a packaging exercise.

What does help is that detection stays in the browser (docs/adr/0005). Only captured JPEGs
cross the wire, not a frame-rate video stream, which is the difference between a web app
that feels usable on a phone and one that does not.

## A PWA is the fit

A manifest, icons, the apple-touch-icon links in `App.razor` and a service worker that
caches the shell and nothing more — there is no point pretending to work offline when the
server is load-bearing. The camera then works properly rather than falling back to the file
input, because installed PWAs get `getUserMedia` on iOS 16.4 and later, so a Stack becomes
available from a phone for the first time.

The blocker is the one the README already names: the camera needs a secure context. Hosting
on a real HTTPS hostname solves the phone problem and the camera problem together. Tailscale
fits a personal catalogue best — a certificate, devices already on the tailnet, nothing
exposed publicly. Cloudflare Tunnel gets a public hostname without opening a port, and
Azure Container Apps is where Aspire deploys if the thing should simply live somewhere.

## Distribution without a store

**Android allows it.** A signed APK hosted anywhere installs once the user permits unknown
sources. Bubblewrap will also wrap the PWA as a Trusted Web Activity, which is a genuine
installable Android app for about an hour of work. Google's developer verification for
sideloaded apps on certified devices was phasing in through 2026 with a free hobbyist tier
and an install cap; check where that landed before relying on it.

**iOS effectively does not**, which is the strongest argument for the PWA. Add to Home Screen
is the only path with no Apple account and no ceiling. Ad Hoc distribution covers a hundred
devices a year, each UDID registered by hand, on the $99 Developer Program — fine for a
household, not for a link on a page. Free personal provisioning expires every seven days and
needs a computer to re-sign. The Enterprise Program is for distributing to employees of a
company of a hundred or more and gets its certificate revoked when used for the public. EU
web distribution under the DMA is real on iOS 17.5 and later but gated behind two years of
good-standing membership and a million first annual installs in the EU.

## The route not to take yet

.NET MAUI Blazor Hybrid reuses the Razor components, which is the appeal, but Hybrid runs
them on the device. The pages reach into `DbContext` and the Discogs services in process, so
every module would have to be split behind an HTTP API and rewritten as a client — weeks of
work, and the compiler-enforced module boundaries would need rethinking along with it.

It only earns that cost if offline capture becomes a goal: photographing a crate somewhere
with no signal and syncing afterwards. Until someone wants that, the PWA is strictly better
value.
