# Part 1965 Windows OTP compiler validation

The compiler release gate completed locally. Repository-wide managed acceptance remains open.

| Check | Actual result |
| --- | --- |
| Pinned release | OTP-29.1.1 at ad05823719d77c8faee87348ea39513d4e2f99c5 |
| Frozen payload | [Source, compiler and runtime](source-freeze.json), [compiler inputs](compiler-source-closure.json), [full launch](full-launch.json) |
| Focused regression | [496 passed](focused-verified.json), [exact coverage in two subruns](fast-splits/receipt.json) |
| Scoped release | [17 Equal and 28 Different](pilot.json) across 45 images |
| Uncached full release | [508 Equal and 858 Different](full-uncached/otp-corpus.json) in 1188.44 seconds. Zero Unsupported, SourceRejected and CompilerError. |
| Raw changes | [All 508 previous exact images retained](full-uncached/compare-summary.json). 0 new exact images. [Section deltas](full-uncached/section-delta.json) record 0 gains and 0 losses. |
| Normalized instructions | [3 gains and zero losses](rank-counts.json), 15 frames closer and 0 farther |
| Native | [146 release observations and 13 synthetic comparisons](native-early-records/receipt.json), [pinned loader outcomes](loader-rollup.json) |
| Remaining causes | [Corpus ranking](ranked-causes.md) |
| Windows build profiles | [Effective profiles agree](profile-equivalence.json) |
| Readiness | [Regenerated from the immutable managed test report](readiness-receipt.json) |

The branch emitter groups identical framed returns and combines initial heap reservation with proven tuple projections. Call input fields that are dead after the next effect stay in X. A Y slot used to construct a nested outgoing tuple is cleared after its last read when each continuation has a fresh value in that slot.

The linked scope and runtime execution use the frozen compiler and release manifests. The full corpus recompiles every module. The native checks preserve callback values and failures, Common Test style assets, and the ordering of protected effects.


The managed Jaro workloads retain the existing timeout blocker. No current full managed run, local delivery commit or server mutation is recorded.
