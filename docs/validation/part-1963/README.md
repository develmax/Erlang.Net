# Part 1963 Windows OTP compiler validation

The compiler release gate completed locally. Repository-wide managed acceptance remains open.

| Check | Actual result |
| --- | --- |
| Pinned release | OTP-29.1.1 at ad05823719d77c8faee87348ea39513d4e2f99c5 |
| Frozen payload | [Source, compiler and runtime](source-freeze.json), [compiler inputs](compiler-source-closure.json), [full launch](full-launch.json) |
| Focused regression | [488 passed](focused-verified.json), [exact coverage in two subruns](fast-splits/receipt.json) |
| Scoped release | [47 Equal and 59 Different](pilot.json) across 106 images |
| Uncached full release | [508 Equal and 858 Different](full-uncached/otp-corpus.json) in 1202.50 seconds. Zero Unsupported, SourceRejected and CompilerError. |
| Raw changes | [All 508 previous exact images retained](full-uncached/compare-summary.json). 0 new exact images. [Section deltas](full-uncached/section-delta.json) record 2 gains and 0 losses. |
| Normalized instructions | [11 gains and zero losses](rank-counts.json), 13 frames closer and 0 farther |
| Native | [107 release observations](native-early-records/receipt.json), [pinned loader outcomes](loader-rollup.json) |
| Remaining causes | [Corpus ranking](ranked-causes.md) |
| Windows build profiles | [Effective profiles agree](profile-equivalence.json) |
| Readiness | [Regenerated from the immutable managed test report](readiness-receipt.json) |

This package keeps only the live selector and catch tag through guarded BER decoder continuations. Tagged tuple alternatives of equal size share checked field projections. The guarded finite return validates both constructors and retains an unframed branch. The independent BER fixture retains one inherited throw stack difference reported in the component receipt.

The linked scope and runtime execution use the frozen compiler and release manifests. The full corpus recompiles every module. The native checks preserve callback values and failures, Common Test style assets, and the ordering of protected effects.

The first linked native run stopped with {error,nofile} when its shared-entry recipe received the wrong fixture directory. The corrected support directory resolved the module in the direct probe and in the complete linked replay. The frozen compiler and corpus inputs were retained.

The managed Jaro workloads retain the existing timeout blocker. No current full managed run, local delivery commit or server mutation is recorded.
