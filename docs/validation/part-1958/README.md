# Part 1958 Windows OTP compiler validation

The compiler release gate completed locally. Repository-wide managed acceptance remains open.

| Check | Actual result |
| --- | --- |
| Pinned release | OTP-29.1.1 at ad05823719d77c8faee87348ea39513d4e2f99c5 |
| Frozen payload | [Source, compiler and runtime](source-freeze.json), [compiler inputs](compiler-source-closure.json), [full launch](full-launch.json) |
| Focused regression | [468 passed](focused-verified.json), [exact coverage in two subruns](fast-splits/receipt.json) |
| Scoped release | [65 Equal and 87 Different](pilot.json) across 152 images |
| Uncached full release | [506 Equal and 860 Different](full-uncached/otp-corpus.json) in 627.11 seconds. Zero Unsupported, SourceRejected and CompilerError. |
| Raw changes | [All 506 previous exact images retained](full-uncached/compare-summary.json). 0 new exact images. [Section deltas](full-uncached/section-delta.json) record 0 gains and 0 losses. |
| Normalized instructions | [23 gains and zero losses](rank-counts.json), 19 frames closer and 0 farther |
| Native | [66 release and 22 synthetic observations](native-early-records/receipt.json), [pinned loader outcomes](loader-rollup.json) |
| Remaining causes | [Corpus ranking](ranked-causes.md) |
| Windows build profiles | [Effective profiles agree](profile-equivalence.json) |
| Readiness | [Regenerated from the immutable managed test report](readiness-receipt.json) |

This package consumes proven BER tag and length results through indefinite and size-split continuations, shares guarded local tails, preserves case selector tuples across effects, and aligns projected frame roots. It also omits unused tuple projections before frame planning when the same clause checks a map. The validators cover binary boundaries, typed branches, error reasons, guarded tail calls, IPv4 and IPv6 loopback, and GUI fixtures.

The initial focused launch exceeded the Windows command-line limit. It failed before starting the test process. The recovered run used the same frozen compiler and test binaries in two sequential processes and verified complete exact coverage. The [original diagnostic](linked-scope.stderr.log) and [bounded subrun receipt](fast-splits/receipt.json) are preserved. The [earlier parent-gate error](full-orchestration.stderr.log) occurred before the uncached release launch and both source identities were retained.

The managed Jaro workloads retain the existing timeout blocker. No current full managed run, local delivery commit or server mutation is recorded.
