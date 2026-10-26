# Part499 — corrected process and node liveness

`erlang:is_process_alive/1` now respects queued signals and pending termination. `erlang:is_alive/0` follows the original node-name and persistent dynamic-name predicate. Complete source partition and all390 original values/full stack profiles match. Final affected regression1386/1386 passes. [Evidence](../validation/part-499/process-liveness/README.md).

| Module | Registered | Verified full | Pending | Known differences |
|---|---:|---:|---:|---:|
| erlang |351|82|256|13|
| binary |31|31|0|0|
| proplists |22|22|0|0|
| queue |38|38|0|0|
| ordsets |21|6|15|0|
| proc_lib |39|0|28|11|
| erts_internal |7|0|6|1|
| lists |91|0|91|0|
| maps |34|0|34|0|

Total708 registered (+0),179 verified (+2),504 pending,25 known. The original559 cohort has61 closed/498 remaining (10.91%); six reclassified pending contracts remain additional work. Original library preparation still covers34 libraries and1321 primary modules; this is not semantic API completion.

Source guards1726 (+3), six production files changed or added. No native ABI/infrastructure promotion. Local9039 readiness is an explicit parent-CI/final-affected composite; exact new-head continuous CI and fresh original captures remain delivery gates tracked in the PR. Broader obligations remain in [the complete inventory](complete-inventory.json) and [checkpoint486](checkpoint-486.md).

Next: audit stateful identity/process table queries (`node/0,1`, `processes/0`, `registered/0`) and continue signal-order contracts, reusing affected blocks and original fixtures. Keep existing library API verification and the25 known differences visible. LICENSE.local.txt stays uncommitted.
