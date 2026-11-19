# Part 1844 — Separate tagged-tuple and atom rejection edges

When a failed untyped tagged tuple test feeds an exact atom fallback
on the same operand and then a case error, the compiler emits the tuple
test first and supplies its type fact on the successful edge. A wrong
tuple tag takes the existing case failure edge. The same proof retargets
immediate projected tuple shape checks while the selector is live.
Wildcard fallbacks keep their previous flow.

The classifier identified 428 counterparts in 210 modules and 238
alternate layouts across 136 modules. The 364-module selection covers
both groups and the representative controls. It took 837.33 seconds
on one worker and reports 10 Equal, 354 Different, zero Unsupported
and zero SourceRejected. It retains the additional ssl_dh_groups exact
image. Compared with Part 1839, 24 normalized functions gain equality
and none lose it. Of 334 changed images, ERTS prepares 332 in a batch.
The two on-load images, crypto and pubkey_os_cacerts, load separately.
The SHA-256 observation and the certificate-cache clear operations
match the preceding and pinned images.

Twenty-five integrated tests pass, including the exact-atom fallback
and wildcard controls. The initial concurrent compiler process ran
out of memory building the test assembly. The bounded retake limits
Roslyn and builds with zero warnings and errors. Its original failure
is preserved.

[Corpus](cohort.json) · [Normalized functions](normalized-delta.terms) ·
[Loader](loader.terms) · [On-load execution](native-execution.eval) ·
[Tests](focused.json) · [Original build error](test-build.log) ·
[Bounded retake](test-build-bounded.log) · [Remaining groups](table-differences.md).
