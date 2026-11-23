# Part 1967 Windows OTP compiler validation

The compiler release gate completed locally. Repository-wide managed acceptance remains open.

| Check | Actual result |
| --- | --- |
| Pinned release | OTP-29.1.1 at ad05823719d77c8faee87348ea39513d4e2f99c5 |
| Frozen payload | [Source, compiler and runtime](source-freeze.json), [compiler inputs](compiler-source-closure.json), [full launch](full-launch.json) |
| Focused regression | [507 passed](focused-verified.json), [exact coverage in two subruns](fast-splits/receipt.json) |
| Scoped release | [9 Equal and 35 Different](pilot.json) across 44 images |
| Uncached full release | [509 Equal and 857 Different](full-uncached/otp-corpus.json) in 1049.42 seconds. Zero Unsupported, SourceRejected and CompilerError. |
| Raw changes | [All 508 previous exact images retained](full-uncached/compare-summary.json). 1 new exact images. [Section deltas](full-uncached/section-delta.json) record 3 gains and 0 losses. |
| Normalized instructions | [46 gains and zero losses](rank-counts.json), 75 frames closer and 5 farther |
| Native | [172 release observations and 13 synthetic comparisons](native-early-records/receipt.json), [pinned loader outcomes](loader-rollup.json) |
| Remaining causes | [Corpus ranking](ranked-causes.md) |
| Windows build profiles | [Effective profiles agree](profile-equivalence.json) |
| Readiness | [Regenerated from the immutable managed test report](readiness-receipt.json) |

A checked tuple stays rooted in its entry register until the frame owns the selected field. Disjoint source clauses separate non-tuple dispatch from a tagged-tuple failure. A covered pure case arm is removed before frame planning. The cons constructor keeps a proven live register alias after a fused heap reservation. Constructor-column traversal plans recursive list helper ordering while preserving source evaluation order.

The linked scope and runtime execution use the frozen compiler and release manifests. The full corpus recompiles every module. The native checks preserve callback values and failures, Common Test style assets, and the ordering of protected effects.


The managed Jaro workloads retain the existing timeout blocker. No current full managed run, local delivery commit or server mutation is recorded.
