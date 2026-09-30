# A played Copy is never deleted

Until now, removing a Copy has deleted it. Once Gigs are recorded, a Copy that has been played is instead kept as a Former Copy: it leaves the Collection but stays attached to every Selection it was played in. A Copy with no Plays is still deleted outright, because a Copy nobody ever played is most likely a wrong scan or a duplicate entry, and keeping it would only clutter the history with records that were never really mine.

This is written down because a reader who finds a "removed" Copy still sitting in the database will reasonably take it for a leak, or for soft-delete applied out of habit. It is neither. It is there because a Play has to point at something. Deleting the Copy would leave holes in the Selections of past Gigs, and it would quietly lower the play count and weaken the Repeat flags for any Master I still own another pressing of. Those are exactly the questions playing out was recorded to answer, and the damage could not be undone afterwards.

## Considered Options

**Refusing to remove a Copy while it has Plays.** Simple and safe, but it leaves the Collection claiming I own records I have sold. The Collection is defined as every Copy I own, and it would stop meaning that.

**Copying the record's details onto each Play,** so the Copy can go and the history still reads correctly. It keeps the Collection honest, but every Play then carries its own copy of artist, title and pressing, which drift apart from the Catalogue. Repeats would have to be matched on text instead of on the Master.

**Two explicit actions,** one for a mistake and one for a record I have parted with. More precise, but it asks me to make a choice the Plays already make for me.

## Consequences

- "Every Copy" and "the Collection" are no longer the same set. Anything that lists or counts the Collection has to leave out Former Copies, and anything that reads Gigs has to include them.
- Buying back a record I sold makes a new Copy. The old one stays a Former Copy on its Gigs, and the two are connected only through their shared Master or Release, which is where Repeats and play counts are judged anyway.
- A Former Copy never goes back into the Collection. There is nothing to restore, because the Copy on the shelf now is a different physical record.
