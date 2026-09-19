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

## Keeping an installed copy current

Installing is the part everyone thinks about; staying current is the part that decides
whether a store-free app is tolerable a year later. A PWA has nothing to solve here — the
installed copy is the site, so it updates when it loads and the service worker's only job
is to not serve a stale shell forever. That is a real advantage over every native route and
is easy to undervalue while the app is new.

An APK does have the problem. Sideloading installs a version and then forgets about it, so
a hand-installed build quietly rots. Obtainium fixes this from the other end: it installs
directly from a GitHub release or a plain APK URL and then checks for updates the way a
store would. A CI job that attaches a signed APK to each release turns that into ordinary
update notifications without anyone running a store. F-Droid and Accrescent are the fuller
channels if it ever needed to be public.

The native frameworks answer it differently, by shipping a shell once and pushing the code
inside it over the air — EAS Update for Expo, Capgo or Appflow for Capacitor, Shorebird for
Flutter. It is a genuinely good mechanism and it is the strongest practical argument for
those toolchains. It is also solving a problem the web does not have.

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

## Rewriting the client buys less than it looks

The obvious follow-up question is whether some other frontend framework unlocks this. It
does not, and the reason is worth stating plainly: no framework changes the platform rules.
Anything producing a signed iOS binary meets the same hundred-device ceiling above, whoever
built it, and Android permits sideloading regardless. The choice moves the tooling around
distribution, never the permission.

What the tooling difference is worth: Expo is the best of it, where EAS Build produces
internal-distribution builds served from a hosted install page — a URL you open on the
phone, which is as close to the original wish as Apple allows, still capped at registered
devices. Capacitor is the shortest path from an existing web app to a self-updating Android
artifact. Flutter builds a fine APK and would mean writing the client again in Dart. Tauri
targets mobile now, but the mobile half is markedly less mature than the desktop one.

None of them are cheap here, because this frontend is not portable. The Razor components
reach into `DbContext` and the Discogs services in process, so any of these means building
the HTTP API *and* rewriting every page in another language — strictly more work than the
two routes below, for a distribution outcome that is identical.

**.NET MAUI Blazor Hybrid** reuses the Razor components, which is the appeal, but Hybrid runs
them on the device. That same coupling applies: every module would have to be split behind an
HTTP API and rewritten as a client, weeks of work, with the compiler-enforced module
boundaries needing rethinking along with it.

**Blazor WebAssembly as a PWA** is the one worth weighing if that split ever happens anyway.
It stays in C#, reuses the components more directly than Hybrid would, and unlike Blazor
Server it can actually run with the host unreachable. That is the single capability the
current architecture cannot reach, and the only one that justifies the cost: capturing a
crate somewhere with no signal and syncing afterwards. Until someone wants that, the Server
app behind a PWA shell is strictly better value than any of this.
