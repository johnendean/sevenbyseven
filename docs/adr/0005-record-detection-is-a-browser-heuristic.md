# Record detection is a browser heuristic, not a model

A Stack captures hands-free: you hold up a record, the app highlights it, captures it by itself, and waits for the next one. The thing that decides a record is in frame is a heuristic running on a canvas in the browser — region size, contrast against the background, and stillness across a few frames. No model is involved. Claude's job is unchanged and begins only once a still frame has been captured, where it reads the sleeve exactly as it does for a single Scan.

This is written down because the rest of the application reaches for Claude whenever it needs to look at a photograph, and a reader who finds a hand-rolled canvas heuristic doing the seeing will reasonably assume it is a stopgap somebody forgot to replace. It is not. The arithmetic rules the model out and will keep ruling it out.

A vision call is a network round trip of roughly one to three seconds and carries a per-call cost. A highlight that tracks a record while you hold it needs re-evaluating something like ten times a second. That is three orders of magnitude apart, and no amount of model improvement closes it: the gap is the round trip, not the inference. Sending one frame per second instead would cost sixty calls a minute to capture ten records and still lag far behind the hand holding the sleeve.

## Considered Options

**Calling the model per frame,** which is the reading of "have the AI highlight what it sees" that the feature was first described in. Rejected on the latency and cost above. Worth recording precisely because it is the intuitive answer and will be suggested again.

**An in-browser ML model** (TensorFlow.js or MediaPipe) would be genuine object detection at frame rate and locally free. No off-the-shelf model has a class for a record sleeve — the nearest in COCO is `book` — so this means training and shipping our own detector. That is a larger project than the feature it would serve, and the payoff over the heuristic is narrower than it sounds: both answer "is a flat rectangular object being held still", and neither can tell a record from a boxed DVD.

**Classical computer vision on the server,** streaming frames up for contour detection. It inherits the round trip that rules out the model, adds a continuous upload from a phone, and buys accuracy the heuristic mostly already has.

## Consequences

- The heuristic cannot tell a record from any other large, still, rectangular object. It will capture a book. This is survivable only because Confirmation is mandatory (docs/adr/0002): a junk capture comes back with no Match Candidates and is abandoned from the queue, so the cost of a false trigger is one wasted identification rather than a wrong Copy.
- An empty frame between records is load-bearing, not a nicety. Stillness is the whole trigger, so without a gap the app cannot distinguish "still the record I already took" from "the next one has arrived".
- Detection degrades where the record does not contrast with what is behind it. Holding a sleeve against a dark background is a user-facing technique the capture screen has to teach rather than a bug to be fixed in the detector.
- A Stack needs a live camera, so it is unavailable in the contexts where `getUserMedia` is — notably a phone hitting a dev machine by IP over plain HTTP. Single `/scan`, with its file-upload fallback, stays the path for those.
- The heuristic's thresholds are tuning constants with no correct value derivable in advance. They belong somewhere they can be adjusted and tested without a browser.
