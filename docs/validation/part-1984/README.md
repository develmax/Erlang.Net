# Part 1984 — Public checked entries, guarded binary tails and mixed generator ordinal

| Gate | Result |
| --- | --- |
| Complete uncached release | 512 Equal, 854 Different, 0 Unsupported, 0 SourceRejected in 841.37 seconds. No new or lost raw matches. [BEAM report](full-uncached/otp-corpus.json) |
| Focused compiler cases | 595 passed. [Cases](focused-verified.json) |
| Scope from the same compile | 79 images, 27 Equal and 52 Different. [Derived scope](derived-scope-retake/scope.json) |
| Native observations | 70 passed in 9.88 seconds. All 17 changed images prepared. [Native report](native-scoped-retake/receipt.json) |
| Normalized bodies | 0 gains and 0 losses. [Rank counts](rank-counts.json) |
| Binary frame widths | zip:skip_bin/2 and prim_zip:skip_iolist/2 both move from 3 Y slots to no frame, matching the pinned allocation vectors. [Function widths](targeted-frame-widths.tsv) |
| Mixed list helper | proplists:split/2 emits the pinned second helper identity. compile:diffable/2 still needs its other lifting prerequisites. [Function key audit](mixed-helper-ordinal.terms) |

The first combined driver omitted the previously required yeccscan image from its generated native selection. Its raw error and original selection remain intact. The corrected selection uses the same complete freshly compiled images and exact compiler SHA. The output was never recompiled for the retried scope, and the corrected native gate runs all 70 observations. The preceding source build error from overlapping C# locals is also retained.
