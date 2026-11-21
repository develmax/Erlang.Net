# Part 1960 Windows OTP compiler validation

The compiler release gate completed locally. Repository-wide managed acceptance remains open.

| Check | Actual result |
| --- | --- |
| Pinned release | OTP-29.1.1 at ad05823719d77c8faee87348ea39513d4e2f99c5 |
| Frozen payload | [Source, compiler and runtime](source-freeze.json), [compiler inputs](compiler-source-closure.json), [full launch](full-launch.json) |
| Focused regression | [479 passed](focused-verified.json), [exact coverage in two subruns](fast-splits/receipt.json) |
| Scoped release | [78 Equal and 111 Different](pilot.json) across 189 images |
| Uncached full release | [507 Equal and 859 Different](full-uncached/otp-corpus.json) in 719.46 seconds. Zero Unsupported, SourceRejected and CompilerError. |
| Raw changes | [All 506 previous exact images retained](full-uncached/compare-summary.json). 1 new exact images. [Section deltas](full-uncached/section-delta.json) record 2 gains and 0 losses. |
| Normalized instructions | [15 gains and zero losses](rank-counts.json), 9 frames closer and 0 farther |
| Native | [34 release observations](native-early-records/receipt.json), [3 guarded receive cases](guarded-receive-native.json), [pinned loader outcomes](loader-rollup.json) |
| Remaining causes | [Corpus ranking](ranked-causes.md) |
| Windows build profiles | [Effective profiles agree](profile-equivalence.json) |
| Readiness | [Regenerated from the immutable managed test report](readiness-receipt.json) |

This package preserves a frameless BER directive read before a framed effect, defers local tuple construction and reuses a dying outgoing argument slot. Grouped matched continuations order checked roots before allocation. Closed recursive entry scheduling reserves slots in pinned OTP order and records the proven integer type for a private positive decrement. Runtime witnesses exercise BER directive handling, retry, mixed dispatch and headless wx.

The initial linked test detected that new operand metadata blocked receive frame localization. [The retake](correction-receipt.json) matches the source register independently of its type annotation and rederives the type after the guard. The scope and runtime runs bind to the frozen compiler, SDK and release manifests. The full corpus recompiles every module.

The managed Jaro workloads retain the existing timeout blocker. No current full managed run, local delivery commit or server mutation is recorded.
