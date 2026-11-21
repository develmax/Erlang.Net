# Part 1962 local frame for a caught terminal clause

Pinned reference is Windows OTP 29.1.1 from `ad05823719d77c8faee87348ea39513d4e2f99c5`. The isolated parent is frozen Part 1960 retake compiler `5d11f43d6e7038f6025ea49389b809754c55b498883a17d8a19b09a0d2420824`. The candidate rebuild is `2a1479be05e2439e960a60d1b41125cb851891568eeec10ea9cae62d90ff6912`. The SDK layout check and strict local replay passed.

Pinned `beam_ssa_pre_codegen.erl:1422–1502` places stack frames at the smallest valid dominating block. The actual `handle_apply/5` SSA first compares the selector in X, and its direct terminal branch needs no frame. The fallback's catch tag is the only Y live range over its ordinary call. The fifth entry value in the OTP source is unused, so the fallback allocation specifies four live X registers. The implementation proves this shape from both clauses, reads each ordinary call against its own register bindings and reuses the existing parallel argument, tail call and catch generators. Repeated binding, guarded and additional pattern shapes fall through to their existing schedules. [Strict replay](replay-files.json) · [Pinned physical specimen](../megaco-monitor-audit-1961/megaco_monitor.precodegen).

The wider Part 1960 first-difference group `is_eq_exact` versus `allocate` has 320 modules. A pinned ordinary tail edge and later catch narrows the visible release cohort to four modules. `wx_object:init_it/6` additionally needs a computed self argument and nontrivial caught body. `snmp:set_trace/2` contains several guarded alternatives. The `dets_v9` witness is a closure with more than two alternatives. The implemented proof closes the direct two-clause shape in `megaco_monitor:handle_apply/5`. [Pinned cohort](pinned-cohort.terms) · [Source selection](source-scope.json).

| Scoped result | Parent | Candidate |
| --- | ---: | ---: |
| Whole-file Equal | 11 | 11 |
| Different | 7 | 7 |
| Unsupported | 0 | 0 |
| SourceRejected | 0 | 0 |
| CompilerError | 0 | 0 |

The uncached 18-module scope took 34.306 seconds with two workers. Only `megaco_monitor` changes image hash, with no whole-file or section gains and losses. Of 41 examined functions in the changed image, 40 are identical to the previous output and `handle_apply/5` gains exact physical and normalized instruction equality. The module remains Different. [Whole-file results](wide-compare-sealed.json) · [Instruction evidence](instruction-delta-sealed.terms) · [Timing](scope-timing.json).

The permanent test passes on the candidate and fails on the exact parent. It covers the selector in two different argument slots and preserves the repeated-variable equality check in a third function. Pinned ERTS loaded the actual release module in all three roles. Five real observations cover the asynchronous notification, handled throw and error, and the bad spawn with the original `handle_apply/5` and `handle_info/2` line numbers. Ten synthetic observations compare success, thrown terms, exit, error stack and repeated matching. [Regression outcomes](focused-sealed-results.json) · [Actual execution](native-megaco-sealed.log) · [Fixture execution](native-fixture-sealed.log).

The first release smoke assertion excluded the retained spawn failure frame. Inspecting the pinned runtime output established both expected frames and the corrected comparison retains them. The full corpus and full managed suite remain linked-package gates. All SDK child processes used the process-local switch that prevents adding the global tools directory to PATH.
