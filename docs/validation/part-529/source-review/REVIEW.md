# Resource-domain570 bounded source review

Packet manifest5AB1B089556C8F03E612E047151BCCDAACE7020C02649179058AFE7D74CF0CA5. Read-only against current w518repair Part528 candidate; all166preimage/addition/payload guards matched. No build/test/CLR/Erlang/native/CI/git mutation. Reviewed lifetime and shared-file composition sources are individually hash/line-bound in review.json.

**No concrete blocking defect established in this bounded source review.** This is not execution approval/certification of all166files or all native callback behavior. Runtime/native tests and exact final-source proof remain required.

## Lifetime and cleanup

* Actual payload uses native allocation with checked size/header fields, type lease, explicit native and root counts. The last transition queues once under payload gate; destructor executes outside that gate and physical memory/type pin retirement follows callback. Unpublished allocation rollback omits destructor on uninitialized C data. Destruction errors are retained and domain drain reports them rather than inventing success.
* Callback version selection obtains an invocation image pin while registry reader/type gate still selects that exact version, then releases those gates before C code. A takeover can change the current version without invalidating an already selected callback's old DSO/private data. This is version/lifetime safety, **not** proof of full UPGRADE callback exclusion semantics; genuine upgrade remains refused.
* Logical purge unlinks only names still owned by the retiring image, under independent name writer; it does not hold binding/name gates while awaiting C or dropping physical pins. Detached live payloads keep old types/DSOs while fresh CREATE gets a distinct type. A type already taken over by a new owner is not selected for old-image unlink.
* Pending load/type/authority publication reserves records and errors before Ready commit. Post-commit cleanup failures are recorded, preventing callers from treating a live published owner as failed/unloadable. Rollback independently releases versions, pending counts, created types, authority and pending-load domain membership.
* VM arena preserves allocations across individual DLL closure. Owner/domain/global-pointer capacity and checked arithmetic precede destructive realloc/free. Final reclaim snapshots actual owners, checks physical image closure outside arena gate, retries on revision changes and requires resource domain physical drain. It does not reclaim old userdata merely because one DLL unloads.

## Guest/root/scheduler composition

Guest publication stages resource and binary custody before replacing slot; failed publication rolls resource admissions back. Native header descriptors own real resource leases, not dummy slab/ERTS words. Collector computes graph/dead sets and checked scalar totals before releasing leases/blocks. Source collector insertion keeps existing567AllocationWords and old PayloadWords meanings. Resource normal-GC retirement is queued once onto an actual normal scheduler auxiliary lane; admitted dispatcher owner keeps physical completion from racing closed lanes. Non-GC origin remains separately dispatched. Final root/physical image cleanup is outside logical DOWN as already designed.

Host boundary requires explicit NativeTermHostScope524. Traversal/retention occurs outside its gate and publication rechecks closing under gate; Dispose closes admission then independently releases stable roots outside the gate. Public/opaque providers do not inherit the reviewed exact image/body capability. Exact delegates/images—not MFA spellings—authorize guest native paths.

## Preserved Part528 scope

Confirmed staged ProcessRuntime preserves NativeMetadataImage527 options assignment and CreateOrdinaryLocal562 factory. Heap storage still includes ordinary references; both checked567reclaim totals and567result field survive collector composition. BEAM exact loader-key registration/map construction stays in place. ReferenceTerm/loader-key new files not touched by resource packet are unchanged dependencies, not reverted primary copies. Existing primitive policy/default/public resource activation remains unchanged.

## Gates and explicit limits

1. Build and run all21new/retained resource controls plus final Part528 reference/map/567controls and prior affected logical/BEAM/host/native regressions on composed sources. Existing source-only packet/passive receipts are not fixture passes.
2. Execute genuine resource C family/fresh-load-after-purge routes through both loader routes and intended runtime profiles with all provider/worker/physical-cleanup receipts. genuine_upgrade must remain explicitly refused until separately implemented, not reported green from a managed mock/type takeover.
3. Compile and verify both managed fixed-table layout and native bridge ABI7/464B append (resource prefix368, cons456), including original prefix/alias/thread-kind controls. Rebind all final partial sources/native assets/source plans before runtime CI.
4. Monitor/stop/down storage, full foreign-thread/API set, allocation during load, upgrade exclusion, VM-fatal boundaries and OS portability remain declared contract limits. Explicit refusal is safer than unowned callbacks but does not close load_nif/2.
5. Native headers/backing domains are actual custody and native bytes; they are not additive process heap word representation. Keep public min/max budgets/process_info/statistics disabled until full headers/stack/funs/binary/offheap/message/ETS/host domains and safepoints are complete. No24promotion follows this review.

Review does not assume arbitrary simultaneous free/use of the same C pointer is safe; native callers must satisfy the documented ownership/synchronization contract. No speculative lock cycle was promoted without a demonstrated acquisition chain.
