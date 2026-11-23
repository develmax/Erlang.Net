# Part 2011 PR 143 record and list tail integration

[Hosted run 45123141134](https://github.com/develmax/Erlang.Net/actions/runs/45123141134) at 373b3192 recorded 35 platform failures. Twelve jobs passed and the core join was skipped. The [local baseline](before.json) preserves the corresponding failure messages and exact case IDs.

Merge source commit 4dfb03e3 into the existing PR head. The merge commit 6592dd48 adds the ten [historical checkpoint artifacts](../part-1830-local-compiler-checkpoint/README.md). The target already implements the list-return forwarding routine and the guarded heap opcode rewrite. The merge retains those implementations and the existing closed changelog volume. All seven cases recorded in the source checkpoint pass on the integrated tree.

Pinned OTP ad05823719 implements frame placement in beam_ssa_pre_codegen:need_frame/1 and emits test_heap for heap allocation without a stack frame in beam_ssa_codegen:cg_alloc/2. After proving disjoint frame ownership, the C# leaf pass removes a zero-slot frame from a straight-line constructor return. It uses the existing allocation writer to retain the heap words and original X roots. Calls, alternate edges inside the leaf, stack operands and live frame slots are outside this pattern.

The existing external-leaf test now compares equal on all bytes. The [file and chunk comparison](external-frame-comparison.txt) and [four ERTS observations](external-frame-native.stdout) separately verify instruction choice, file equality, load and behavior for integer, atom/external tail and fallback inputs. The synthetic fixture is distinct from the installed release corpus. The [paired generator disassembly](generator-lines-otp-and-managed.txt) gives the five source lines used by both compilers. Update those expectations and retain the helper controls. The additional improper-source helper in OTP remains a layout difference.

| Check | Report | Result |
| --- | --- | --- |
| .NET SDK 10.0.400 test project build with no restore and one MSBuild worker | [Build](build-final.json) | Passed in 91.4 s |
| Exact previously failing cases, seven source-checkpoint cases, whole-module control and installed-release control | [44 cases](focused.json), [raw output](focused.stdout), [time](focused-timing.json) | 11 passed and 33 failed in 40.7 s. Two recovered failures and nine passing controls. |
| Changed C# layout and passive source admission | [Layout](style-release.txt), [13 plans](source-admission-final.json), [hash refresh](refresh-final.json) | Passed |

Reuse the existing C# ownership analysis, writer and tests and the Apache-2.0 OTP development oracle. No dependency, public runtime contract or registry classification changed. Release corpus totals keep their historical scope. Full uncached corpus, readiness regeneration from a complete passed test report and final CI acceptance remain open. Prioritize failed prepared-map patterns and shared frame ownership in the remaining 33 cases.
