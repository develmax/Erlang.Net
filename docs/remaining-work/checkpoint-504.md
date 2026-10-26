# Part504 — complete native map callback surface

| Module | Registered | Full contracts | Pending | Known differences |
|---|---:|---:|---:|---:|
| erlang |351|83|255|13|
| binary |31|31|0|0|
| proplists |22|22|0|0|
| queue |38|38|0|0|
| ordsets |21|6|15|0|
| proc_lib |39|0|28|11|
| erts_internal |7|0|7|0|
| lists |91|0|91|0|
| maps |34|0|34|0|

Total708 registered,180 full,504 pending and24 known differences. MFA/full deltas0/0. The original559 cohort remains62 closed/497 open, plus seven reclassified contracts. Catalog9073 (+18); source guards1772 (+41).

Parts502/503 are merged; main30d294e5c CI40400648174 is green. Part504 binds all15 selected Windows native-map callbacks together (+11), with pending graphs, persistent flat/HAMT structure, copying and iterator lifetime. Complete440639-byte original output matches both loaders, including168 added mutable-key/replacement cases. New18/18, final native/term1706/1706 and retained501–503 original checks pass locally. [Evidence and explicit limits](../validation/part-504/native-map-family/README.md).

All24 known differences remain open. Actual Y-root ownership and a native term payload/collector substrate are implemented, but guest producers and all live root domains still need integration before heap/GC/options can be exposed. Queue/statistics and debug_flags fixes cover shared branches without closing their entire APIs. Native family materialization still needs post-return topology/equality/hash integration; a prepared Linux original resource batch requires execution. Exact published-head CI/review are delivery gates.

Next: integrate retained native-map topology through MapTerm and nested-key operations; connect guest term ownership/root/collection domains in shared batches; close the24 differences against full source contracts, then validate remaining504 contracts. Preparation of1321 original images is separate from semantic acceptance. No justified whole-project completion percentage is available.

Review/CI correction: parent19ecb1ed9 Windows CI passes9073/9073, while Linux observer fails because file output waits behind its intentionally held dirty IO worker. Defer writes until actual release, validate18 rejected diagnostic mutations and rerun the unchanged five-case protocol. Restore indexed immutable-map lookup (old allocation controls0/2 → four new controls4/4); retain pinned CMP-first collision behavior after genuine1151-byte whole original/ordinary/reviewed comparisons. Current catalog9077 (+4 review groups), guards1772; all24 differences and504 pending contracts remain open. [Correction evidence](../validation/part-504/review-and-linux-correction/acceptance.json).
