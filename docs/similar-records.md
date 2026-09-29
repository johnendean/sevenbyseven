# Finding records like this one

Nothing here is decided. This is where the idea stood in September 2026, prompted by
TypeSafe AI's Jev model (<https://typesafe.ai>), written down so the same ground does not
have to be covered again from scratch.

## The question that comes first

"Similar" means several different things, and which one is wanted decides everything
below. Other Releases of the same Master, "more like this" while browsing the Collection,
and Tracks that would mix well one after another are three different features. Until one
of them is chosen there is nothing to build.

## Most of it needs no model at all

The Catalogue already holds what the clear-cut kinds of similarity need. Other pressings of
the same album share a `DiscogsMasterId`. Label, genres, styles, country, release year and
Track BPM are all columns. A query over those answers "same Master", "same label" or "same
style, same decade" exactly, and it should be the first step whatever else is added.

## What Jev is, and what it is not

Jev does not search. It has no index and no embeddings, so it cannot be handed the
Collection and asked what resembles a Release. Each call takes a *state* (text or JSON) and a
set of typed questions, and answers each with a number rather than prose:

- **Noul** — a yes/no question answered as a probability between 0 and 1.
- **Score** — a rating against a rubric of 2 to 10 levels.
- **Choice** — one of up to 255 named options, with probabilities.

Choice and Score answers carry a confidence value, so the caller can decide when to trust
an answer and when to show nothing. Several questions can go in one call.

It is a single REST endpoint, `POST https://api.typesafe.ai/v1/systemone`, with a bearer
token. There are Python and JavaScript SDKs but no .NET one; a typed `HttpClient` in the
style of the Discogs client would be all that is needed. Documentation is at
<https://docs.typesafe.ai>, with an index at `/llms.txt`.

## Where it would fit

The pattern is the one TypeSafe's own re-ranking cookbook describes. A cheap query over the
Catalogue produces a shortlist of 20 to 50 Releases sharing a style, label, era or BPM range.
Jev is then asked about each pair — the Release being viewed as the state alongside one
candidate — something like "would someone who likes A reach for B?", and the shortlist is
sorted by the answer. Candidates below a confidence threshold are dropped rather than shown.

Cost is not a consideration. A lookup of 30 pairs at around 500 tokens each is roughly
15,000 input tokens, a fraction of a cent at the $42 per billion quoted in September 2026.

## Where it earns its place

On the fuzzy kinds of similarity only: same feel, same scene, would sit well in a set after
this one. Shared tags rank those poorly and a pairwise judgement could do better.

Two limits hold regardless. Re-ranking cannot recover a record the shortlist missed, so the
query in the first step still decides what can ever be shown. And Jev sees only what it is
sent — it never hears the music, and its documentation does not say how much it knows about
artists — so its judgement is bounded by the Discogs metadata plus whatever BPM has been
entered by hand (docs/adr/0003 explains why that is sparse).
