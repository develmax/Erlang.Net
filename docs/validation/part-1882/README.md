# Prove generic map-pattern input types

All 44 selected cases out of 9,840 pass, including the disabled
SSA control compiled through the existing direct admission path.
The 250-file uncached comparison takes 612.86 seconds with
6 Equal, 244 Different, 0 Unsupported and 0 SourceRejected. The
6 byte controls retain equality. One source-derived Map
annotation is emitted at each actual lexical map-pattern extraction
dominated by its explicit tag check and stable saved input.

All nine predicted raw Type gains materialize on the current emitted
images. Relative to the earlier retained multi-stage audit 10 tables
become exact and none lose equality. The same-scope comparison
against Part 1875 gives nine Type gains, eight Dbgi gains and
one LitT gain with no losses. The 37-path exact direct parent
overlap has 35 changed outputs, zero section gains or losses and
no normalized gains or losses. The counts use distinct baselines.

All 242 regular changed images in the Part 1875 comparison
prepare on pinned ERTS. Two on-load modules, crypto and zstd,
load through their normal runtime entry. Crypto direct and incremental
hashes match. Zstd produces the same compressed binary and restores
the original payload. The native map fixture has 14 identical values
and exception reasons covering absent keys, non-map inputs, nesting,
and exact numeric key/value distinctions.

The broader type census identifies 2,966 immediately guarded
untyped sites in 240 modules missing the map type and 1,383 same-MFA
typed counterpart functions in 233 modules. These are candidate
witnesses. Current Code and whole-file equality remain independent.

[Source identity](source-freeze.json) · [Release status](pilot.json) ·
[Actual Type tables](map-type-table-results.json) ·
[Per-section exact baseline comparison](compare-1875/pilot-section-delta.json) ·
[Native map fixture](native-map/receipt.json) ·
[Normal on-load transcripts](actual-on-load/).

