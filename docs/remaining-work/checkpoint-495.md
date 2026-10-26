# Part495 — map BIF contract audit

`erlang:map_size/1`, `erlang:map_get/2` and `erlang:is_map_key/2` now have source branch/key relation audits and matched original OTP-29.1.1 compiled observers. The final corpus contains194 values and970 full-stack profiles:1158 literal whole outputs and6 complete outputs of a source-bound interior-node protocol. No fields are normalized after capture. Source proofs, original receipts and failures are retained in [evidence](../validation/part-495/map-bifs/README.md).

The opt-in test encoder calls public `erlang:term_to_binary` inside the owning runtime. The earlier generic encoder produced different large-map byte order despite equal complete decoded values/stacks. Original golden files remain unchanged. No production BIF change was necessary. All100 inherited BIF groups and3 new groups pass103/103 on final local sources. The local9006 readiness report is explicitly composite with the independently verified parent CI9003. Continuous exact-head GitHub CI, packages, original reacquisition and cold consumers remain delivery gates.

The real interior HAMT node is accepted by `is_map` but is not a complete map head. Its defined `map_size` rejection is compared with every stack frame intact, binding only that actual node to a source role. Native lookup through `erts_maps_get` asserts a complete hashmap head; undefined native memory interpretation of an interior node is outside this contract. Supported raw cursor lists over complete maps remain mandatory and unchanged.

| Module | Registered | Verified full | Pending | Known differences |
|---|---:|---:|---:|---:|
| erlang |351|76|262|13|
| binary |31|31|0|0|
| proplists |22|22|0|0|
| queue |38|38|0|0|
| ordsets |21|6|15|0|
| proc_lib |39|0|28|11|
| erts_internal |7|0|6|1|
| lists |91|0|91|0|
| maps |34|0|34|0|

Global registered708 (delta0), full173 (delta+3), pending510, known25. Original559 cohort:55 closed (9.84%),504 remain; six reclassified contracts are additional pending work. These figures describe default registered contracts, not every original module export or total project completion. Other module counts remain unchanged. Original catalog34 libraries/1321 primary modules and176 regex captures retain separate evidence.

Next: continue pending erlang contracts grouped by shared primitive dependencies, while retaining the 60/40 implementation/audit balance. The25 recorded differences, full original application lifecycles, native ABI/platforms, compiler/scheduler completeness and performance comparisons remain open. LICENSE.local.txt remains uncommitted. No merge of Parts486–495 is implied.
