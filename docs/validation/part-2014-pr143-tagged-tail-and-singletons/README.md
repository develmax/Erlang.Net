# Part 2014 PR 143 tagged dynamic tails and constructor singletons

[Hosted CI at 6daff03e3](https://github.com/develmax/Erlang.Net/actions/runs/45139711481) passed 7,816 of 7,844 platform cases. The [raw failure headlines](hosted.txt) record all 28 failures.

The tagged dynamic case now admits a recognized non-allocating binary BIF fallback and a permutation of the original incoming registers. Preserve the original registers along failed tuple and atom checks. Reuse the parallel register-move scheduler on the dynamic path after the branch-local zero-slot allocation. The fallback is frameless. Pinned OTP sys_core_fold simplifies maps:get/2 to erlang:map_get/2. Reuse the existing C# map rewrites and BIF classification with the pinned Apache-2.0 compiler as the oracle. No dependency or runtime API changed.

The constructor singleton check compares the whole generated image with the deterministic pinned BEAM and retains a disabled-inference guard. The reference was compiled with OTP-29.1.1 at ad05823719 from the exact test source with binary and deterministic options. Its SHA-256 is 478BE6FB63F553B35185C01DD0092E5212F422A1E026500A080314FF42DF450F. The [disabled reference disassembly](singleton-disabled.txt) retains the separate two-tuple allocation for local, mixed and closure-reachable constructors.

| Check | Report | Result |
| --- | --- | --- |
| .NET SDK 10.0.400, one worker, no restore | [Build](build-final.json), [initial overload error](build.stdout) | Final build passed in 106.0 s. |
| Same 44 selected cases | [Results](focused.json), [raw output](focused.stdout), [time](focused-time.json) | 18 passed and 26 failed in 45.3 s. Two new passes and no lost passes. |
| Deterministic fixture parity and original ERTS execution | [Native probe](native.eval), [results](native.stdout) | Two complete synthetic BEAMs equal and 14 paired calls agree, including badkey and badmap. |
| Three uncached release representatives with dynamic module calls | [Baseline](baseline-corpus.json), [candidate](candidate-corpus.json), [hash changes](release-changes.json), [preparation](release-prepare.stdout) | 0 Equal, 3 Different, 0 Unsupported and 0 SourceRejected on both. Generated hashes are unchanged and all three images prepare in pinned ERTS. |
| C# layout and passive admissions | [Style](style.txt), [13 plans](source-admission-final.json), [bindings](refresh-final.json) | Passed. |

The previous full release ledger keeps its historic scope. Full CI, final uncached release corpus and readiness regeneration remain open. Prioritize prepared-map failure paths and shared frame ownership within the remaining 26 cases.
