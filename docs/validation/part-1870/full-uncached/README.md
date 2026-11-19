# Part 1870 complete uncached Windows corpus

The exact source freeze from [the scoped report](../README.md) produced
409 Equal, 957 Different, 0 Unsupported and 0 SourceRejected in
492.11 seconds across four fixed shards. There are no gained or lost
whole-file matches against Part 1865. The compiler changed 406 images.

Pinned ERTS prepared 405 changed images. The ordinary crypto on-load
path succeeds for the reference, the preceding candidate and this
candidate. Direct and incremental SHA-256 agree in all three.
The two native tail-copy fixtures also agree.

Eighty-seven Dbgi sections and one LitT section gain raw equality.
There are no section losses. All 957 residual files differ in Code.
There are 182 residual Dbgi differences. The original effective build
profile identity agrees for all 1,366 sources. The 197 exact logical
include maps are archived separately. The generated LDAP source
changes physical path and retains both input hashes and its logical path.

The normalized exact-file comparison records 19 gains and 12 losses
across 2,972 comparable changed bodies. It records 7 nearer and 192
farther frame distances. The three known scoped losses are in the
same full result. The additional nine losses require triage with the
exact pinned, preceding and current instruction sequences. Managed
whole-repository and readiness acceptance have not been run here.

[Raw full comparison](otp-corpus.json) · [Timing](timing.json)
· [Section gains](section-delta.json) · [Runtime](loader-summary.terms)
· [Normalized summary](normalized-summary.terms) · [Exact losses](lost-bodies.terms)
· [Pinned input verification](input-verification.json).

[Ranked remaining groups](remaining-causes.md) · [Rank timing](rank-receipt.json).
