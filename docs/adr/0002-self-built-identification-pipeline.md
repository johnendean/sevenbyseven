# Build the identification pipeline ourselves

No API exists — from Discogs, MusicBrainz, or anyone else — that takes a photograph of a sleeve or centre label and returns a matched release. The consumer apps that appear to do this build their own vision layer on top of the Discogs API. So we build the pipeline: decode a barcode when one is present, otherwise send the photograph to a multimodal model and have it extract artist, title, label, catalogue number and any visible year, then search Discogs on whichever of those fields we obtained.

Identification is never accepted automatically. Every Scan presents three to five Match Candidates for Confirmation, which removes the need for confidence scoring and keeps wrong pressings out of the Collection.

## Considered Options

**Reverse image search** (Google Cloud Vision web detection) answers "which album", which is the question I can already answer by looking at the record in my hand. It cannot distinguish the original pressing from the reissue, which is the question that matters.

**Classical OCR alone** (Tesseract or similar) is free and deterministic but fails on the hand-drawn and stylised typography common on older sleeves, and records pressed before roughly 1982 carry no barcode at all — a substantial part of the collection this is being built for.

**Auto-accepting the top match** was rejected because reissues frequently share sleeve art across a dozen Releases. A library quietly full of wrong pressings is far more expensive to repair than a confirmation tap is to make.

## Consequences

- Sleeve photographs are discarded once a Scan ends. Only the Release's own image reference is retained.
- Vision extraction costs a small amount per Scan and can produce a plausible but wrong catalogue number. Confirmation is the safeguard, which is part of why it is mandatory.
- Barcode decoding is the exact path and is tried first, but is unavailable for most pre-1982 records.
