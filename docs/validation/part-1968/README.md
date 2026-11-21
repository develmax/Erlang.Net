# Part 1968 Windows OTP compiler validation

The compiler release gate completed locally. Repository-wide managed acceptance remains open.

| Check | Actual result |
| --- | --- |
| Pinned release | OTP-29.1.1 at ad05823719d77c8faee87348ea39513d4e2f99c5 |
| Frozen payload | [Source, compiler and runtime](source-freeze.json), [compiler inputs](compiler-source-closure.json), [full launch](full-launch.json) |
| Focused regression | [512 passed](focused-verified.json), [exact coverage in two subruns](fast-splits/receipt.json) |
| Scoped release | [13 Equal and 66 Different](pilot.json) across 79 images |
| Uncached full release | [509 Equal and 857 Different](full-uncached/otp-corpus.json) in 1067.79 seconds. Zero Unsupported, SourceRejected and CompilerError. |
| Raw changes | [All 509 previous exact images retained](full-uncached/compare-summary.json). 0 new exact images. [Section deltas](full-uncached/section-delta.json) record 2 gains and 0 losses. |
| Normalized instructions | [29 gains and zero losses](rank-counts.json), 84 frames closer and 5 farther |
| Native | [192 release observations and 28 synthetic comparisons](native-early-records/receipt.json), [pinned loader outcomes](loader-rollup.json) |
| Remaining causes | [Corpus ranking](ranked-causes.md) |
| Windows build profiles | [Effective profiles agree](profile-equivalence.json) |
| Readiness | [Regenerated from the immutable managed test report](readiness-receipt.json) |

Call-local lifetime analysis removes trailing duplicate stack roots while preserving the surviving alias. The terminal case emitter partitions tested tuple arities from the atom edge. The deferred list constructor evaluates the ordinary call before building safe list heads. Literal cons patterns enter comprehension-helper scheduling through the same recursive decomposition as explicit cons patterns.

The linked scope and runtime execution use the frozen compiler and release manifests. The full corpus recompiles every module. Native checks cover callback effects and stacks, Mnesia case arms, assembly and loading of a real record chunk, optimizer execution and list-generator effects. The trailing-root witnesses expose four private functions through temporary copies with only ExpT changed and assert identical Code. The loader rollup uses the unmodified compiler images.


The managed Jaro workloads retain the existing timeout blocker. No current full managed run, local delivery commit or server mutation is recorded.
