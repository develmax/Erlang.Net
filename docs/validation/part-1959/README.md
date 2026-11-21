# Part 1959 Windows OTP compiler validation

The compiler release gate completed locally. Repository-wide managed acceptance remains open.

| Check | Actual result |
| --- | --- |
| Pinned release | OTP-29.1.1 at ad05823719d77c8faee87348ea39513d4e2f99c5 |
| Frozen payload | [Source, compiler and runtime](source-freeze.json), [compiler inputs](compiler-source-closure.json), [full launch](full-launch.json) |
| Focused regression | [474 passed](focused-verified.json), [exact coverage in two subruns](fast-splits/receipt.json) |
| Scoped release | [77 Equal and 107 Different](pilot.json) across 184 images |
| Uncached full release | [506 Equal and 860 Different](full-uncached/otp-corpus.json) in 721.71 seconds. Zero Unsupported, SourceRejected and CompilerError. |
| Raw changes | [All 506 previous exact images retained](full-uncached/compare-summary.json). 0 new exact images. [Section deltas](full-uncached/section-delta.json) record 1 gains and 1 losses. |
| Normalized instructions | [36 gains and zero losses](rank-counts.json), 21 frames closer and 1 farther |
| Native | [56 release and 14 synthetic observations](native-early-records/receipt.json), [pinned loader outcomes](loader-rollup.json) |
| Remaining causes | [Corpus ranking](ranked-causes.md) |
| Windows build profiles | [Effective profiles agree](profile-equivalence.json) |
| Readiness | [Regenerated from the immutable managed test report](readiness-receipt.json) |

This package reuses proven BER result fields, preserves the protected decoder boundary, shares checked tuple projections, and places grouped tuple and map checks ahead of their frames. The wx changes align nested record initialization and alias moves. The release witnesses cover malformed BER, native compression, checked dispatch and headless wx behavior.

The initial focused test conflated a generic frame trim with the specialized BER path. Pinned OTP and the linked compiler preserve the unknown-selector case failure. The retake rebuilds the test assembly and binds the scope and native witnesses to the same compiler, runtime, inputs and release manifest. The machine reboot interrupted the first full run. The [original launch record](interrupted-20261120-1354/full-launch.json) remains archived and the final run recompiles every module.

The ex_gauge normalization census records one wider frame in do_init/1. Four physical local-entry labels shift by one while the local function set stays identical. The LocT loss remains tracked in the [raw names and labels](gauge-local-table-raw.terms).

The managed Jaro workloads retain the existing timeout blocker. No current full managed run, local delivery commit or server mutation is recorded.
