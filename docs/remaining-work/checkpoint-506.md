# Part506 checkpoint

| Module | Registered | Full contracts | Pending | Known differences |
|---|---:|---:|---:|---:|
| erlang |351|83|255|13|
| proc_lib |39|0|28|11|
| binary |31|31|0|0|
| proplists |22|22|0|0|
| queue |38|38|0|0|
| ordsets |21|6|15|0|
| lists |91|0|91|0|
| maps |34|0|34|0|
| erts_internal |7|0|7|0|

Total708 registered,180 full,504 pending,24 known. Delta API/full/known0/0/0. All24 differences are coordinated through shared storage, roots and resource policies; none is marked closed by a selected observer.

1. Done: Parts502/503 merged into main. Parts504/505 implement the selected15 native map callbacks together, including pending maps, flat/HAMT transitions, mutation/iteration/copy and materialized return/reentry. Parent504 exact CI is green;505 source-inventory correction has new exact CI pending.
2. Current506: actual private tuple/list payload, source/trampoline/dictionary/host roots, physical process retirement and source-send escape repair. Genuine whole335-byte original values/stacks match ordinary and private runtimes. Final full9097/9097 and both native loader routes pass; exact-head CI remains required.
3. Prepared507: exact trusted binding provenance, no-copy source calls/dictionary aliases and BEAM register roots. Receive snapshot/callback/cancellation root lifetimes must be complete before activation. These source proposals are not executed proof.
4. Prepared508: pinned heap-size/default conversion policies and real private generation collection. Public policy/metrics enforcement must wait for complete roots and payload domains.
5. Next common gates: message/native/binary/map/function domains, precise liveness, resource policies and all six coordinated original contract groups; then re-audit the remaining504 pending APIs.

[Evidence and explicit limits](../validation/part-506/owned-guest-roots/README.md). Catalog9097 includes14 new finite groups; source guards1804. No arbitrary whole-project percentage is assigned.
