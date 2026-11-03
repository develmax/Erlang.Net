# Part536 propagation and binary halt — immutable local evidence

## Part536 addendum — 2026-10-29 — Parent propagation and binary halt retirement

Integrate the reviewed parent193e correction, retaining its immutable native-pending cache and strict live-versus-dead cancellation controls. Close the child binary shutdown gap: stop admission synchronously on VM halt, guard early process completion and join the actual binary drain in both VM-HALT completion paths. Ordinary process exit acquires no global binary wait.

A clean missing-close RED accepted an additional real reader after RequestImmediateHalt; bounded recovery settled. The corrected control covers public Host immediate halt and spawned real erlang:halt/2 with flush=false. An already-held reader preserves the real native byte; new admission is refused, process completion and HaltQuiescence wait, and releasing the reader frees the arena and invalidates native acquisition through the returned Host descriptor. Portable reference identity remains readable. Earlier fixture/reflection/frame/identity setup failures remain separate from this meaningful RED.

Final Release passes181/181 in one approved outside-sandbox child. Debug is an explicit composite:179/181 inside sandbox, with two PortProbe launch EACCES failures retained, then2/2 on the exact unchanged Debug DLLs outside sandbox. This is not an uninterrupted Debug181 run. Fresh original binary619 two modes564 and regex613 five modes524 execute successfully. All seven complete raw streams, including complete errors and stacks, equal the final managed1088 outcomes independently in Release and in a subsequent four-group Debug replay. Protected paired streams bind each actual child window and DLL hashes; the overwritten captures485 file is never attributed to the earlier Debug composite. Cached original observers were executed fresh; no fresh observer-compilation or original physical-header instrumentation is claimed.

The earlier94-group checkpoint and archive remain historical evidence. Final source freeze binds58 child C# files plus4 parent correction files (62 union). Canonical counts remain708 registered/180 verified/504 pending/24 known; API and full-contract deltas0/0. Local selected evidence does not replace exact-head review, full CI, merge or main CI. Full P3/restart/match-spec and P4 monitor/timer/function-image work remain open; artifact-only proposals are not delivered APIs.

Evidence is retained in this directory; see [the archive manifest](manifest.json).

The paired subdirectories preserve literal original/managed ETF bytes and exact execution windows. The local subdirectories retain EACCES failures, setup failures, the clean missing-close RED and corrected two-route GREEN. Source-only review and the62-file snapshot are distinct from actual execution. Earlier docs/validation/part-536 is preserved unchanged. Current GitHub acceptance is not asserted by this archive.
