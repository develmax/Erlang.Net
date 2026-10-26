# Part496 — record predicates and original BEAM native records

`erlang:is_record/1,2,3` now have full supported-term source audits and matched OTP-29.1.1 compiled observers. The new corpus contains670 values and3350 complete stack profiles:3984 predicate outcomes plus36 constructor/metadata outcomes. Every capture is compared whole, without projected fields or normalization. [Evidence](../validation/part-496/record-bifs/README.md).

The original native-record syntax initially exposed real admission gaps: nonempty `Recs` metadata and local `put_record` constructors were refused. Decode valid record definitions, retain declared field order, default-versus-absent values and export flags, and publish them through the existing module descriptor. Execute the observed local constructor against its actual code image, reading all inputs before overwriting an aliased destination. Add `is_record/3` to the existing bounded `bif3` admission. Do not claim external/update constructor forms or complete native-record instruction coverage.

Final local affected regression passes221/221:103 inherited BIF groups,14 new groups and104 relevant BEAM/native-record checks. Retain0/13 before repair,8/13 after metadata support and13/13 after local construction and BIF admission; supplemental1/1 verifies defaults, definitions, exportedness and aliasing. Original compiler refusals for variable-tag guards and a bitstring default are retained as fixture preparation errors. Dynamic/error arguments remain ordinary BIF calls; legal literal guards are tested separately.

| Module | Registered | Verified full | Pending | Known differences |
|---|---:|---:|---:|---:|
| erlang |351|79|259|13|
| binary |31|31|0|0|
| proplists |22|22|0|0|
| queue |38|38|0|0|
| ordsets |21|6|15|0|
| proc_lib |39|0|28|11|
| erts_internal |7|0|6|1|
| lists |91|0|91|0|
| maps |34|0|34|0|

Global registered708 (delta0), verified176 (+3), pending507, known25. Original559 cohort58 closed (10.38%),501 remain; six reclassified contracts are additional pending work. No `records` module API receives compatibility credit from the supporting controls. These are default registered contract counts, not every original module export or a whole-project percentage.

Ten changed/new production files are bound; source guards1708→1720 include four new files and eight previously unguarded dependencies. Reviewed native source-plan rows/seals advance only where their previous hashes match the parent; historical rows remain explicit. Native ABI signatures, infrastructure roster and its bound files are unchanged. Original acquisitions preserve the6427-file provider and settle their owned processes. Eight unchanged-source acquisitions are reused locally with exact source hashes; CI reacquires all14 groups independently.

Parent Part495 exact-head CI40268601688 is independently green9006/9006. Local9020 readiness is an explicit parent/final221 composite; continuous new-head CI, packages, cold consumers and cleanup remain publication gates. The published PR records their eventual exact-head outcome without rewriting frozen evidence. LICENSE.local.txt stays uncommitted; no merge is implied.

Next: `is_builtin/3`, then separate stateful contracts for `is_process_alive/1` and `is_alive/0`. Keep25 known differences and the broader compiler/native ABI/boot/distribution/lifecycle/performance backlog visible. Other module boundaries remain as [checkpoint495](checkpoint-495.md).
