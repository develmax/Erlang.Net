# process_info/1 completion audit

Read-only source/evidence audit; no Main/build/native/Git/network changes. All OTP observations below are source-derived from OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. Bound source hashes and prior evidence are in source-bindings.json.

## Result

The existing internal helper already supplies a coherent thirteen-field logical default query, including PID/runtime ownership, terminated target handling, one owner snapshot and omission of registered_name when unregistered. Public registration currently exposes only process_info/2. The exact pinned native default list contains seventeen entries; the four absent resource fields are total_heap_size,heap_size,stack_size,garbage_collection. There is no hidden ready physical accounting backend in the inspected Runtime.

Important contract nuance: pinned erlang.erl5535-5540 says default tuple order is undefined and not all tuples mandatory, and5565-5568 labels /1 debugging-only. Thus the old internal property name UnavailableMandatoryFields and historical notes are stricter than the public default-query specification. Four fields are absent relative to pinned source behavior, not unconditional mandatory items under every compliant implementation. A documented thirteen-field process_info/1 subset is an honest small API increment; it must remain explicitly partial against the pinned seventeen-field implementation. This does not relax process_info/2 physical queries, and it is not erlang completion.

## Reusable implementation and actual prior evidence

ProcessInfoDefaultCatalog.OrderedKeys matches the pinned pi_1_keys sequence (erl_bif_info.c1007-1025), while LogicalItems filters exactly the four missing fields. QueryDefaultLogicalProcessInfo uses QueryProcessInfo once and removes only {registered_name,[]} for the default case. QueryProcessInfo already routes a cross-process owner snapshot through the process mailbox/signal protocol and rechecks termination. RequireProcessInfoTarget preserves foreign CLR owner rejection, fixed-local wire identity, stale and remote domains. Do not replace this with independent per-field reads.

Part281 evidence-receipt records 6561 passed/zero failed and a stored Passed row for runtime/process-info-logical-default-and-dynamic-owner-isolation. Current/archive source equality and selective archive receipt hashes were checked; this audit did not rerun the suite. That HOST verifies thirteen-field projection/order, registered omission/inclusion, dead result and dynamic runtime isolation. Eight default-query original templates exist as an unpromoted candidate/source blob; preparation is not native execution. They include a strict seventeen-key expectation and cannot become a common subset comparison unchanged.

## Four resource blockers

missing-fields.json records source formulas and absent model ownership. Heap size is actual process heap capacity, not object liveness. Total heap additionally includes old heap, buffer and on-heap message fragments. Stack size is stack words, not frame count. The GC list contains target process settings and genuine minor generation state, not the CLR's global collection counters.

ProcessRuntime.GarbageCollection currently requests GC.Collect on the managed heap; it does not own process-specific young/old generations. Counting its requests as minor_gcs would invent an accounting model and would miss automatic collections and major resets. Shared immutable Term objects and mailbox references have no per-process heap owner. BEAM register frames contain real Y-slot arrays, but they are neither an ERTS heap nor an authoritative full process stack across compiled source, BEAM, tailcalls and exceptions. test_heap currently checks live registers without reserving/collecting a native process heap. Existing AllocationDiagnostic intentionally reports managed thread/process diagnostic allocations; it is not an Erlang heap meter.

## Smallest honest next increment

Register /1 through the existing logical projection, return undefined for absent local processes, preserve domain errors and omit unregistered registered_name. Document exactly thirteen supported logical defaults, four omitted resource fields and unchanged /2 rejection for them. Compare common typed projections against OTP; do not compare raw default key order, exact reductions or runtime-dependent stack/heap numbers. Add HOST omission checks that intentionally differ from native, keeping those checks out of the shared comparable corpus. This is API presence with a supported subset, not full default parity.

Completing exact pinned resource behavior is a separate runtime project: establish real per-process heap/fragment ownership, actual stack-word representation across every entry route, GC settings and enforced collection/generation transitions; snapshot those coherently through existing owner protocol. A real managed word arena with its own capacity and collection state could be researched, but a numerical shadow counter over CLR allocations is not proof of physical equivalence. No bounded source-only registration can honestly manufacture those four fields. Resource proof must use allocation/liveness/send/receive/major/minor/tail/unwind cases, not numeric equality to unrelated CLR totals.
