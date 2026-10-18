# Part371 final local acceptance

All **21 ordsets APIs** are implemented and checked together against pinned OTP-29.1.1. API implementation and complete-contract closure remain distinct: **3/21 ordsets contracts are complete;18 still require dependency closure**. The remaining obligations include complete arbitrary-term/map/FUN ordering, private lists:usort paths and relevant length/trapping behavior. The full-module finite packet passes; it is not a proof of those general domains.

| Module | Present APIs | Complete contracts | Known differences | Pending contracts |
|---|---:|---:|---:|---:|
| ordsets |21/21|3|0|18|
| erlang |351/351|20|13|318|
| binary |31/31|31|0|0|
| queue |38/38|38|0|0|
| proplists |22/22|22|0|0|
| lists |91/91|0|0|91|
| maps |34/34|0|0|34|
| proc_lib |39/39|0|17|22|

Delta: **+21 APIs/+7 complete source contracts/+31 tests**. Global699 APIs/18 modules;114 complete,30 Known differences,555 pending. The seven source-complete promotions are erlang:hd/1,tl/1,tuple_size/1,is_integer/3 and ordsets:new/0,is_empty/1,to_list/1. All other existing classifications/evidence are retained, with496 current source guards (454 unchanged,eight reviewed updates,34 dependencies).

Original full21 [GitHub CI39113248779](https://github.com/develmax/Erlang.Net/actions/runs/39113248779) succeeds. Final Main matches **2616/2616** observations:436 values/2180 full-stack profiles at default/0/1/2/8 depths,0 normalized fields,0 relations. Shared corrections port84 source clauses/19 unique-merge states, preserve actual lists dispatch, fix continuation coalescing/operator annotations and narrow map/FUN comparison order.

The independent erlang family in failed parent39112151003 is admitted only through its own successful steps and complete raw provenance; the parent and failed ordsets8 collector remain failed. Final erlang new packet matches4506/4506 strictly. Replayed old erlang9582+264=9846 also matches strictly. No original OTP rebuild was needed for retained regression.

Queue/proplists:8117 examined,8087 accepted full-field/source-bound observations,7683 strict,0 unexpected differences,68 corruption controls.18 original-only diagnostics and12 own-topology rows stay explicitly unpromoted. Binary:3850 accepted/3852 raw (3814 strict+36 live-reference bindings); historical supplement214/216 remains failure for two unmatched launch contexts. Independent review proves both difference rows and allfour raw supplement ETF files identical to the previous final capture. No normalization or new waiver hides them.

Final canonical run passes **7673/7673** in349.640s, retaining all7642 old ordered names. First restricted run7657/7673 in377.031s retains16 port-launch eacces failures; four unchanged controls and then the whole suite pass outside sandbox on identical DLLs. No port runtime change. Metadata/package suffix54.343s passes11 integration outcomes,two local packages,PackageReference consumer and installed CLI. Five readiness files match byte-for-byte; all five runtime/compiler DLL hashes remain unchanged. Restore123 pre-existing emitter-reset Limits notes by MFA key without losing21 new registry rows. Source layout and native backend gates pass.

Durable raw evidence, validators, actual Main captures, native provenance/member bindings, negative controls and source snapshots are under ../evidence. Relocation indexes preserve historical source paths; raw failures are never rewritten. Prior369/370 seals are revalidated. Central attribution is ../source-attribution.md and ../../../OTP-PORTS.md. LICENSE.local.txt is excluded; no public NuGet publication or default-branch merge. Finished implementation publication and its exact-head CI are the next Root step, separate from these local gates.
