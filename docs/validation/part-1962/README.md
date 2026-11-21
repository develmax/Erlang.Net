# Part 1962 Windows OTP compiler validation

The compiler release gate completed locally. Repository-wide managed acceptance remains open.

| Check | Actual result |
| --- | --- |
| Pinned release | OTP-29.1.1 at ad05823719d77c8faee87348ea39513d4e2f99c5 |
| Frozen payload | [Source, compiler and runtime](source-freeze.json), [compiler inputs](compiler-source-closure.json), [full launch](full-launch.json) |
| Focused regression | [486 passed](focused-verified.json), [exact coverage in two subruns](fast-splits/receipt.json) |
| Scoped release | [98 Equal and 179 Different](pilot.json) across 277 images |
| Uncached full release | [508 Equal and 858 Different](full-uncached/otp-corpus.json) in 1460.70 seconds. Zero Unsupported, SourceRejected and CompilerError. |
| Raw changes | [All 508 previous exact images retained](full-uncached/compare-summary.json). 0 new exact images. [Section deltas](full-uncached/section-delta.json) record 1 gains and 0 losses. |
| Normalized instructions | [14 gains and zero losses](rank-counts.json), 89 frames closer and 0 farther |
| Native | [74 release observations](native-early-records/receipt.json), [pinned loader outcomes](loader-rollup.json) |
| Remaining causes | [Corpus ranking](ranked-causes.md) |
| Windows build profiles | [Effective profiles agree](profile-equivalence.json) |
| Readiness | [Regenerated from the immutable managed test report](readiness-receipt.json) |

This package preserves case-arm list proofs and confines a catch frame to its protected branch. Checked wx callbacks reuse the existing entry-effect planner. The projection allocator recognizes terminal returns and excludes registers needed on a failed guard before assigning scratch values. Permanent tests cover named and wildcard tuple fields, retained inputs, disabled SSA, and the affected branch behaviors.

The linked scope and runtime execution use the frozen compiler and release manifests. The full corpus recompiles every module. The native checks preserve callback values and failures, Common Test style assets, and the ordering of protected effects.

The managed Jaro workloads retain the existing timeout blocker. No current full managed run, local delivery commit or server mutation is recorded.
