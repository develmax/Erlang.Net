# Part505 — native maps after return

| Module | Registered APIs | Full contracts | Pending | Known differences |
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

Total708 registered,180 full,504 pending,24 known. MFA/full/known delta0/0/0. All15 selected Windows native map callbacks are implemented together; other platforms and complete NIF ABI acceptance remain separate. Source guards1778 (+6). Current catalog9083 (+6 over corrected504), including4 materialization and2 physical duplicate reentry groups.

Completed: merge502 and503 into main, native map family implementation, preserved pending graph aliasing, source-style flat/HAMT mutation, iterators and copy; immutable saved map layouts after native return, nested hashing and iterative exact comparison; cached target-VM lookup after source-owner closure; duplicate physical flat reentry red0/2 then green2/2. A separate3898-byte genuine duplicate reentry protocol matches both loaders after actual GC. Actual original32-case batch matches both native loaders and both whole raw profiles; physical ownership/provider checks pass. Earlier2930 affected successes are historical; final affected native/terms/processes run passes2936/2936. Parent504 Linux retry passes5/5; final parent Windows and505 exact-head CI remain delivery gates.

Final tooling passes935/935.

Next measurable gates: publication and exact-head CI/review. [Evidence](../validation/part-505/native-map-materialization/README.md).

Shared24-contract work remains active: integrate genuine guest tuple/list payload and explicit root handoff, then remaining closure/dictionary/message/native/external roots; bind real process collection, heap options/statistics/reports together; compare complete source contracts before reclassification. The reviewed opt-in506 production slice is being prepared independently; it is not current default behavior or a closed contract. Remaining504 pending APIs follow this common substrate. No measured overall-project completion percentage is established.
