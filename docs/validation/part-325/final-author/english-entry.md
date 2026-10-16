## Part 325 — Retain accepted connected LINK output across producer death

2026-10-16 (Europe/Moscow)

**Part status:** bounded retained-LINK implementation and actual Main/full/native/readiness validation completed; current original325 execution and whole erlang compatibility remain partial.

### Behavior and original source

Public guest link/1 and link/2 now accept an immutable LINK packet, its captured connection generation, local relation and local receiver priority into finite connection-owned staging before the caller waits. Once accepted, the existing entry-owned worker retains its ticket and resource lease across producer cancellation or kill. LINK remains ahead of the terminal linked EXIT or a subsequent UNLINK_ID; a worker must not discard an accepted LINK because the originating process died or its local relation became inactive.

Normal async_dist=false callers wait for publication through the existing cooperative owner-control wait. With async_dist=true they return after bounded acceptance. A hard staging-cap refusal raises system_limit before installing the relation. Encoding happens outside the runtime gate, followed by atomic validation and acceptance; concurrent inbound LINK remains idempotent, and existing retirement/relink/pending-unlink identity protections remain intact. Accepted UNLINK and the existing shared worker architecture are unchanged.

This corrects a source-confirmed phase gap. At pinned OTP29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5, bif.c315–380 requests no_trap and sends LINK before yielding. dist.c3201 and3649–3798 encodes and appends the control before suspending the caller; the retained context no longer owns that queued buffer. async_dist ignores the busy threshold. The SDK instead uses finite separately accounted staging/transport budgets and CLR tasks. This is a source-guided ownership adaptation, not physical buffer, scheduler-phase or general wire equality.

### Actual focused validation and preserved failures

Actual revised-source target builds passed with zero warnings/errors. Eighteen isolated HOST groups passed, retaining the seventeen prior groups and adding five retained-LINK controls within one new group. Normal and async capacity/turn scenarios execute genuine generated guest calls, public queries and inbound kill signals, observe killed DOWN and guest unwinding before draining, then decode LINK followed by terminal EXIT. A hard-cap control checks no local relation or packet on refusal. Existing ghost-unlink/relink, generation/retirement, owner pumping and accepted UNLINK controls remain part of the preserved scope.

The identical EF3D HOST source over exact historical323 production compiled successfully and produced two clean semantic REDs: NormalCapacity fails its missing LINK assertion at line82, and AsyncCapacity fails its missing early return-marker assertion at line63. Both owned fixtures then settled through unchanged cleanup. These are actual missing-behavior failures, not build, reflection or timeout substitutes. Source freezes6172/329B (GREEN) and578C/0D07 (RED), Root guards5FC1/123A and the independent actual review are retained.

Earlier mixed RED1 and RED2 are also preserved. RED2's async primary assertion was accompanied by a nested cleanup timeout after public kill was accepted. Cleanup revision3 removes only premature catch-path waits: the existing Finish first closes the owned connection, then joins the producer and helpers. Production code and successful assertions did not change. No precise earlier owner-signal failure cause is inferred, and the mixed failures are not relabeled as clean REDs.

### Original325 collector — source only

The separate reviewed collector has fourteen verified pinned source objects and ninety-three pure synthetic refusal/validation controls. Its two future fresh three-VM scenarios distinguish normal target busy from async publication after a presaturated filler. B is actually stopped and resumed with owned PID/PGID identity checks. C kills only A; A must observe killed DOWN before B resumes. A separately compiled B receiver must then receive the nonce-bound actual linked EXIT from A with reason killed. Empty link tables alone are not evidence of retained LINK.

Three source-built image MD5 values must be attested on every original VM. Raw primary and cleanup failures, bounded STOP/CONT/process cleanup and compiler/source receipts are retained separately. E159 author freeze/A628 plan and independent6C43 review are source-only: no original325 compiler/VM/live-node execution or native retention result is claimed here. Existing original323 busy/query/kill observations do not prove the new retention contract.

### Actual final validation

Actual Main passed **6693/6693** tests and all eleven integration/package checks outside the known native named-pipe sandbox restriction. Every6692 previous ordered test remains present. All22 earlier custom MFA limitations were restored before updating only link/1, link/2 and the obsolete325 follow-up phrase in process_info/2. Unlink/1, process_flag/2, process_flag/3, process_info/1 and every unrelated registry row remain exactly the published parent values. Registry509 / previous509 / delta0 and351erlang is API presence, not semantic completion.

Three fresh separate post-full trusted Windows roles passed: legacy callbacks, five-phase live-runtime SDK-private allocation query, and three reallocation observations. These do not execute original Erlang or measure physical memory. The actual readiness CLI exited0 against immutable324. Final source authority binds23 owned paths/seven C# files/2366 complete inputs,1109 legacy and1134 reallocation inputs with exact canonical LF/workflow pins and unchanged import/ABI policies.

See [tests](../validation/part-325/final-main/tests.json), [full guard](../validation/part-325/final-main/full-guard.json), [native guard](../validation/part-325/final-native/guard.json), [readiness](../validation/part-325/module-readiness.json) and [receipt](../validation/part-325/evidence-receipt.json). The new original325 normal/async retention scenarios remain unexecuted; future source-plan/VM/compiler attestations are not observed outcomes.

### Historical remote checkpoint

Published323 now has all five independently typed terminal workflows passing at head3785. This part copies only its new lean supplements, without recopying older history or binary/runtime archives. CI, native and original distribution results retain their own raw scopes. The original reallocation observations retain actual source recipes/import inspections and requested/backing ledger boundaries.

Ordinary323 passed5885 contracts plus eight independent fd observations; all229 Differential source files and5885 ordered source/hash/caller/policy contracts are preserved from322. One existing map-print stdout-order flip causes the fresh strict comparator to refuse raw equality. Its raw JSON and strict refusal remain alongside the semantic pass; no stdout normalization occurs. libc versus dotnet_native_memory allocation profiles and their distinct nine-key memory snapshots remain separate actual backend observations, without physical or whole-memory equality claims. These previous-head results do not execute the new325 original retention scenarios.

### Remaining scope and delivery

Pending/autoconnect, remote names/aliases, synchronous embedding setters, full inbound priority, live SDK network peers, general native queue/scheduler/wire parity, physical memory and whole erlang compatibility remain incomplete. API presence is not module completion. This slice updates only link/1 and link/2 limitations plus removal of the obsolete325 follow-up phrase in process_info/2; all other parent limitations, including unlink/1 and process_flag/2,3, remain exact.

Append this completed entry to active0050 after the actual gates, preserving its existing324 text and closed0049 byte for byte. Final evidence includes the full raw test/integration/source closure, fresh native process captures, actual readiness and an explicit copy index/receipt. The evidence receipt is publication authority; this draft and planned remote counts are not.
