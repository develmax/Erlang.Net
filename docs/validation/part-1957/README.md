# Part 1957 Windows OTP compiler validation

The compiler release corpus gate completed locally. Repository-wide managed acceptance remains open.

| Check | Actual result |
| --- | --- |
| Baseline | OTP-29.1.1 at ad05823719d77c8faee87348ea39513d4e2f99c5 |
| Source, tool and runtime identities | [Frozen source](source-freeze.json), [compiler inputs](compiler-source-closure.json), [corpus launch](full-launch.json) |
| Focused compiler tests | [462 passed](focused-verified.json) |
| Pilot | [134 release modules](pilot.json). [Both normalized regressions restored](retake-pilot-receipt.json). |
| Uncached full corpus | [506 Equal, 860 Different](full-uncached/otp-corpus.json), zero Unsupported, SourceRejected and CompilerError, 778.24 seconds |
| Raw gains and losses | [Delta since accepted 503](full-uncached/compare-summary.json). All 506 previously observed raw matches remain. |
| Instructions | [Normalized differences](full-uncached/normalized-summary.terms), [counts](rank-counts.json), [no lost provisional gains](full-uncached/interim-normalized-retention.terms) |
| Raw sections | [Section changes](full-uncached/section-delta.json), [remaining sections](full-uncached/residual-sections.json) |
| Native loader | [432 prepared, 2 shared on_load restrictions](loader-rollup.json) |
| Native execution | [44 release and 10 synthetic observations](native-early-records/receipt.json). Identical earlier witness images and retested images remain separately recorded. |
| Effective Windows build profiles | [Equivalent](profile-equivalence.json) |
| Next groups | [Ranked causes](ranked-causes.md) and [function counts](rank-family-counts.tsv) |
| Readiness | [Regenerated](readiness-receipt.json) using the unchanged immutable managed report and current compiler scope |

The private argument optimizer preserves a sole same-register atom use at the first ordinary call. A known external function reference is a literal and leaves the direct accumulator input intact. The linked package returns exact predicate values without redundant branches, forwards a proven BER binary match context and sinks a projection to the only branch that needs it. The two earlier compiler errors and the original instruction failures are recorded in [Part 1955](../part-1955/README.md) and [Part 1956](../part-1956/README.md). The initial Part 1957 scope, prior to both corrections, remains under [preliminary linked evidence](preliminary-linked/pilot.json).

The final raw run compiles every release file without result caching. Reference BEAM bytes are used for comparison and native execution. The runtime and upstream reference checkout are unchanged in this work part. The managed Jaro workload still exceeds its ten-second test deadlines in the recorded investigation. No repository-wide managed rerun, commit or publication is recorded.
