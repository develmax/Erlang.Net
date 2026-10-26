# Contract completion checkpoint487

This checkpoint tracks the requested re-audit of31 recorded differences and completion of the original559 pending contracts. Source review is recorded for all31; it is not31 complete fresh runtime differential proofs. Six obsolete captured-function defect classifications return to Needs verification. The other25 known differences remain open.

Nine native BIF contracts (bit_size/1, byte_size/1, is_function/2 and six numeric operations) pass source/partition/dependency/effect review and2016 whole original comparisons, including an additional code-literal identity control. The initial559 cohort is **9 closed /550 remaining (1.61%)**. Six reclassified historical defects add6 further full audits. Current ledger: **708 registered /127 verified /556 pending /25 known**. This measures registered contracts only; it is not whole-project completion.

Final-source local regression and publication-head CI are required before delivery. The detailed [audit](../validation/part-487/contract-reaudit/README.md), [31 findings](../validation/part-487/contract-reaudit/known-differences-review.json) and [original559 cohort](contracts487-cohort.json) preserve exact source bindings, original acquisitions and the distinction between finite witnesses and contract closure.

| Module | Registered | Verified | Pending | Known |
|---|---:|---:|---:|---:|
| erlang | 351 | 30 | 308 | 13 |
| lists | 91 | 0 | 91 | 0 |
| maps | 34 | 0 | 34 | 0 |
| records | 8 | 0 | 8 | 0 |
| io | 1 | 0 | 1 | 0 |
| gen_server | 10 | 0 | 10 | 0 |
| supervisor | 14 | 0 | 14 | 0 |
| logger | 26 | 0 | 26 | 0 |
| application | 4 | 0 | 4 | 0 |
| error_logger | 2 | 0 | 2 | 0 |
| proc_lib | 39 | 0 | 28 | 11 |
| sys | 2 | 0 | 2 | 0 |
| erpc | 3 | 0 | 3 | 0 |
| rpc | 2 | 0 | 2 | 0 |
| proplists | 22 | 22 | 0 | 0 |
| queue | 38 | 38 | 0 | 0 |
| binary | 31 | 31 | 0 | 0 |
| ordsets | 21 | 6 | 15 | 0 |
| erts_internal | 7 | 0 | 6 | 1 |
| math | 1 | 0 | 1 | 0 |
| string | 1 | 0 | 1 | 0 |

## Next dependency batches

1. Continue pure erlang BIF source partitions: numeric conversions/rounding, binary/tuple primitives and error metadata. Use shared original worker/support and whole captures; retain overflow/term/guard and caller-context boundaries.
2. Audit common ordering/equality and lists private helpers before closing dependent maps/ordsets contracts. Callback/async/resource/error paths remain distinct from successful collection results.
3. Complete process spawn/request, info, GC/heap controls and source function/lifecycle contracts, then proc_lib/gen/sys/gen_server/supervisor/application dependencies. Do not call CLR estimates identical native heap accounting.
4. Complete side-effectful distribution/NIF/ports/ETS/logger contracts with real source owners, ownership transfer, cancellation settlement and platform checks. Keep unregistered original exports and whole application/library acceptance visible separately.

The broader project backlog,1321-image catalog, source/tool layout and existing original-library acceptance remain described by [checkpoint486](checkpoint-486.md). This audit does not reopen completed image preparation or add a C# rewrite of a pure OTP module. Independent scenarios may share preparation and aggregate failures; dependent captures are blocked after preparation failure. Re-run affected blocks during development and full regression on the final publication source.
