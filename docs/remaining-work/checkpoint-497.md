# Part497 — native BIF identity

`erlang:is_builtin/3` matches the original OTP-29.1.1 source contract and4866 whole observations, including full error stacks. Exactly553 native table entries and the3 instruction special cases equal the existing managed catalog. Preserve the original signed32-bit narrowing of native-small arities, including negative and large aliases. Validate all argument types before lookup. Ordinary module load/delete/NIF code addresses do not change the native BIF flag; the source audit follows all flag initialization/assignment paths. [Evidence](../validation/part-497/builtin-bif/README.md).

Fifteen new groups pass; final affected regression164/164 retains117 earlier BIF groups and32 related function-query checks. No runtime correction is required. Eighteen whole outputs bind only actual owned HAMT-node identity;4848 compare literally. The original last-page missing-apply-proof preparation refusal is retained independently and repaired without changing goldens. All original collectors preserve6427 files and settle their owned processes.

| Module | Registered | Verified full | Pending | Known differences |
|---|---:|---:|---:|---:|
| erlang |351|80|258|13|
| binary |31|31|0|0|
| proplists |22|22|0|0|
| queue |38|38|0|0|
| ordsets |21|6|15|0|
| proc_lib |39|0|28|11|
| erts_internal |7|0|6|1|
| lists |91|0|91|0|
| maps |34|0|34|0|

Registered708 (delta0), verified177 (+1), pending506, known25. Original559 cohort59 closed (10.55%),500 remain; six reclassified pending contracts are additional work. These are default contract counts, not a whole-project percentage. The34 original libraries still prepare1321 primary modules; this is catalog admission rather than full semantic acceptance.

Three unchanged query dependencies are newly guarded:1720→1723. No production, native ABI, reviewed native plan or infrastructure-roster change. Parent496 exact-head CI40276836899 is independently green9020/9020. New local9035 readiness is an explicit parent-CI/final164 composite; exact new-head continuous CI, fresh15 original acquisitions, packages, consumers and cleanup remain delivery gates recorded in the PR.

Next: stateful `is_process_alive/1` and `is_alive/0` audits, then continue dependency-prioritized contracts. Broader compiler/native ABI/boot/distribution/lifecycle/performance obligations and25 known differences remain visible in [checkpoint486](checkpoint-486.md) and the [complete inventory](complete-inventory.json). LICENSE.local.txt remains uncommitted; no merge is implied.
