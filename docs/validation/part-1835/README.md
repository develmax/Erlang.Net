# Part 1835 — Remove entry spills from an owned branch frame

The local candidate extends identical Part 1834 compiler inputs and
uses pinned Windows OTP 29.1.1. It reuses the disjoint-frame ownership
walk to admit entry-copy elimination when a reachable frameless
branch returns before the only allocation. Mixed joins, handlers
and trim instructions retain their existing path. The entry-value
analysis checks uses before clobbers and the existing compactor
removes the resulting unused slots.

The saved-output audit identified 116 removable definitions in 93
functions across 67 modules. There are 108 exclusively dead
definitions across 92 functions in 66 modules. The selected
witnesses include `format_lib_supp:get_format/2` with two slots
reduced to one and `test_server:cast_to_list/1` with one reduced
to zero. [Proof census](frame-proof.json) · [Exclusive cases](frame-proof-exclusive.json).

Both tuple case lowering paths now reuse unchanged constructions.
`megaco_tcp_sup:start_accept_child/2` matches the complete pinned
disassembly, including the allocation, constructor sequence and
tagged branch return. The actual supervisor-call protocol checks
success, explicit error and malformed result. All three outcomes
agree across the compared images. Guarded fresh field aliases retain
the original selector in `snmp_app_sup`. Unary integer conversions
add the proven unbounded integer type after successful resolution.

The 24 paired modules preserved 7 Equal and 17 Different with zero
Unsupported and zero SourceRejected. The first two-worker candidate
took 18.46 seconds. The accompanying baseline took 17.02 seconds.
Thirteen changed images prepared in ERTS and three functions gained
normalized equality. No whole-file or normalized match was lost.
The layout-only rebuild passed the same seven focused tests. All 17
saved differing images and generated Erlang/header contents agree
byte-for-byte. The raw ELDAP report retains its per-run output path.
[Comparison](layout-corpus-equivalence.json).

The comparison tool has an optional SHA-bound cache of diagnostic
instruction views. The paired 17-image test measured 8.50 seconds
with the original script, 9.40 seconds with a cold cache, and
0.49 seconds with a warm cache. All three rank outputs match byte
for byte. The key includes the BEAM, script and ERTS reader identity.
Production compilation and uncached release comparison use source.
[Diagnostic timings](integrated-rank-timing.json).

[Corpus](final-cohort.json) · [Changes and sections](cohort-delta.json) ·
[Normalized functions](cohort-normalized.terms) · [ERTS](cohort-loader.log) ·
[Megaco execution](megaco-combined.log) · [Focused tests](final-focused.json).

The complete uncached corpus and repository regression are pending
on the linked final candidate.
