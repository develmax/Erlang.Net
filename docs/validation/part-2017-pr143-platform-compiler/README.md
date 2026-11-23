# Part 2017 — PR 143 platform compiler failures

[Hosted run 45156814316](https://github.com/develmax/Erlang.Net/actions/runs/45156814316) at `dab948a3773b01299f7d3b17e6a0b0d71805f3bb` passed 7,819 of 7,844 platform checks. The [25 failure lines](hosted-platform.txt) were preserved. The catalog lane passed in that run. The previous catalog transport failure remains a recorded observation with no proven cause. [Part 2016](../part-2016-pr143-computed-key-trim/hosted-catalog.txt).

| Reported checks | Common area | Resolution and pinned compiler evidence |
| ---: | --- | --- |
| 14 | Frame placement, retained arguments, and projections | Reuse existing argument, entry-effect, and frame planners. Measure retained list production before the effect in the layout probe. Keep only one list root across the effect. Respect frame closure and live projection roots. Synthetic decoder fixtures use valid atoms, function headers, and real live inputs on rejection edges. Pinned `beam_ssa_pre_codegen` places frames and determines saved values. |
| 4 | Dispatch classification | Select distinct tuple or string heads before arm-specific work. Use a proven singleton entry for closed atoms. Accept a two-way classifier represented by exact equality with an exclusive alternate. Pinned `beam_core_to_ssa`, `beam_ssa_type`, and `beam_ssa_codegen` provide matching decisions. |
| 3 | Map defaults and covered guards | Mark statically bound `maps:get/3` during Core lowering so copy and import resolution preserve provenance. Retain protected guard checks after the static `maps:get/2` primitive rewrite. The pinned `sys_core_fold` preserves separate key-miss and bad-map paths. |
| 4 | Binary bindings, list generators, and range facts | Check the actual lower-bound operands of named additions. Exercise binding, guard, and improper-tail behavior where pinned OTP emits different valid opcode layouts across options or caller domains. |

The implementation reuses the existing assembler, source binding resolver, call planner, decoded instruction analysis, and runtime host. Apache-2.0 OTP 29.1.1 at `ad05823719d77c8faee87348ea39513d4e2f99c5` supplied compiler source, original files, and native execution. No dependency, public API, runtime registry, or reference checkout change.

| Check | Evidence | Result |
| --- | --- | --- |
| Local exact registered cohort | [Final JSON](focused.json), [stdout](focused.stdout), [prior 43/44](prior-focused.json) | 44 of 44 passed, with 25 restored and 0 lost compared with the previous 19/44 selection |
| `.NET` Debug test project build with SDK 10.0.400 and one build worker | [Build](build.log), [identities](validation-identity.json) | 91.97 s, 0 warnings, 0 errors. Scoped style, build, and selection together took 178.51 s |
| Matched ERTS execution | [Five final synthetic modules](native-behavior.txt), [eval](native-behavior.eval), [intermediate terminal map](native-terminal-map.txt), [intermediate computed map](native-computed-map.txt) | 25 final calls matched across values, error reasons and effects. The two intermediate map probes matched for 12 calls. Synthetic whole-image comparisons are recorded separately. |
| Uncached installed Windows release sample | [Baseline](release-baseline.json), [final](release-final.json), [inputs and output hashes](validation-identity.json), [timings](release-final.timings.json) | 2 Equal, 9 Different, 0 Unsupported, 0 SourceRejected. No new or lost equality, 0 changed output hashes. Final comparison 20.92 s with one worker. |
| Release loader and opcode ranking | [ERTS preparation](release-load.txt), [baseline opcode groups](rank-baseline.terms), [final opcode groups](rank-final.terms) | All nine differing generated images accepted by ERTS preparation. Opcode and allocation-operand groups stayed the same. |
| Source layout and source admission | [Style](style.txt), [source bindings](source-refresh.json), [admissions](source-admission.json) | Style and 13 passive source plans passed. These admissions did not execute native workflows. |

Within the nine Different release files, the leading opcode groups are allocation-root initialization with different frame widths (6 modules, 19 functions), tuple extraction versus allocation (6 modules, 13 functions), and missing function identities (5 modules, 79 functions). These are first-instruction categories. They guide analysis of frame placement and helper formation. The linked raw report identifies modules and functions, and the corpus records sections separately.

The mandatory hosted matrix, its final-head review, and the uncached acceptance run over all 1,366 release files are pending. No readiness change is claimed from the focused suite. Continue the required PR matrix and preserve any newly surfaced result.
