# Part 1961 Windows OTP compiler validation

The compiler release gate completed locally. Repository-wide managed acceptance remains open.

| Check | Actual result |
| --- | --- |
| Pinned release | OTP-29.1.1 at ad05823719d77c8faee87348ea39513d4e2f99c5 |
| Frozen payload | [Source, compiler and runtime](source-freeze.json), [compiler inputs](compiler-source-closure.json), [full launch](full-launch.json) |
| Focused regression | [482 passed](focused-verified.json), [exact coverage in two subruns](fast-splits/receipt.json) |
| Scoped release | [96 Equal and 156 Different](pilot.json) across 252 images |
| Uncached full release | [508 Equal and 858 Different](full-uncached/otp-corpus.json) in 745.70 seconds. Zero Unsupported, SourceRejected and CompilerError. |
| Raw changes | [All 507 previous exact images retained](full-uncached/compare-summary.json). 1 new exact images. [Section deltas](full-uncached/section-delta.json) record 10 gains and 0 losses. |
| Normalized instructions | [5 gains and zero losses](rank-counts.json), 5 frames closer and 0 farther |
| Native | [53 release observations](native-early-records/receipt.json), [6 synthetic failure and effect cases](cold-recovery.json), [pinned loader outcomes](loader-rollup.json) |
| Remaining causes | [Corpus ranking](ranked-causes.md) |
| Windows build profiles | [Effective profiles agree](profile-equivalence.json) |
| Readiness | [Regenerated from the immutable managed test report](readiness-receipt.json) |

This package aligns the ordering of checked projections, preserves the live record owners of deferred values and shares return construction across matching branches. It also places pure tuple construction outside protected calls, records the source line of a malformed generator and rebuilds the line table after moving an error block.

The initial linked regression exposed older case paths where a selector holds a tuple tag and case_end needs the full value in X0. The retake restored that shape and retains the additional proof for an unchanged selected register. The linked scope and runtime execution use the frozen compiler and release manifests. The full corpus recompiles every module. The complete pinned wx gauge image is compared separately from callback values and exception stacks.

The managed Jaro workloads retain the existing timeout blocker. No current full managed run, local delivery commit or server mutation is recorded.
