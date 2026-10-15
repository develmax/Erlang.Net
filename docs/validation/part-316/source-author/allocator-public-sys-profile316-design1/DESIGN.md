# Next increment: two genuine public allocator queries on an explicit CLR profile

Design only against actual Main315 source (Root reports full6642+11; the local full guard is bound separately). No implementation, Main/Git action, build, SDK/native/VM execution or public registration is included here. Pin: OTP29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5. Deliver alloc_info/1 and alloc_sizes/1 next; do not spend another part adding private counters alone.

## Supported public profile

Implement a runtime-owned immutable **CLR system-allocation metadata profile**, advertising one supported OTP query identity: sys_alloc. It describes the actual System.Runtime.InteropServices.NativeMemory backend already used by owner Allocate/Reallocate/Free, call environments and callback tables. It does not relabel joined NIF payload requests as the entire system allocator, binary_alloc, ERTS blocks or carriers. The315 SDK-private two-counter provider remains separate.

Positive public results:

    erlang:alloc_sizes(sys_alloc)
      -> [{instance,0,[]}]
    erlang:alloc_info(sys_alloc)
      -> [{instance,0,[{options,[{e,true},{m,dotnet_native_memory}]}]}]
    erlang:alloc_sizes([sys_alloc])
      -> [{sys_alloc,[{instance,0,[]}]}]

Sizes-empty is the real pinned sys_alloc behavior (erl_alloc.c3155–3213), not a fabricated zero-size statistic. Full info is nonempty, identifies the actually selected SDK backend, and reports that backend facility enabled even when no payload block is live. Do not invent version/call/carrier/maximum fields, tp or tt values, or a malloc-usable size. Do not run a fresh allocation just to answer metadata. The NativeMemory controls in315 establish the backend allocation path separately; new tests must invoke the newly registered public functions.

**Explicit CLR adaptation:** OTP hardcodes `{m,libc}` here (3190–3207). The SDK returns `{m,dotnet_native_memory}` because source proves that API, not a specific libc implementation or malloc tuning. Missing unsupported tp/tt is deliberate: the pin includes them only when the backend exposes nonnegative settings. This pilot offers truthful metadata and exact sizes shape, not byte equality of all allocator options or whole ERTS allocator compatibility.

## Name catalog and capability behavior

Separate recognition from capability. The catalog contains the exact pinned allocator-name set accepted by erl_alloc.c3391–3418, including its four explicit special branches; derive the enum-generated names from the pinned allocator definitions rather than from the set of implemented providers. Before freezing implementation, take ONE bounded source extract of the definition feeding erts_alc_a2ad (generated allocator-types input) at the same pin. Current frozen C source proves the enum/table lookup but does not enumerate every table string; do not fill the gap from memory or treat all other names as unknown. This is a small catalog prerequisite, not a new inventory/project.

Catalog states:

| State | Public behavior |
| --- | --- |
| Not recognized by the pin | badarg after whole-list validation |
| Recognized, implemented and enabled | dispatch the actual supported provider; initially sys_alloc only |
| Recognized, unsupported by this CLR profile | error:notsup for the whole valid request; no partial reply |
| Implemented backend explicitly disabled by a real backend configuration | false; no such state is advertised or manufactured in this pilot |

Recognized unsupported names (for example binary_alloc/eheap_alloc/alloc_util) must not become false, empty stats or unknown/badarg merely because the SDK lacks them. The notsup result for those valid names is a declared partial-profile adaptation and is **not** claimed to match the default OTP run. Support is not inferred from enum membership, zero counters or the NIF library capability flag. The host backend metadata capability is independent of loading an untrusted NIF or installing a default native instruction handler.

## Exact wrapper contract retained

Reuse AllocatorQueryRequest + AllocatorQueryMailbox + its owned producer, not a second receive loop. An outer nonatom/nonlist gives function_clause before making a reference; atom wraps/unwraps a singleton; a Cons/Nil path makes its reference then validates the complete proper nonempty list of unique recognized atoms. Empty, duplicates, unknown atom, nonatom member or improper tail give badarg. Validate the ENTIRE name list before capability checks: `[eheap_alloc,unknown]` is badarg, not an early notsup. A valid recognized unsupported list gives notsup before any reply is posted. Producer failure/cleanup follows the existing actual owned operation.

One real producer posts one SYSTEM reply with instance0 and request-order entries. This is one SDK metadata producer, not a pretend ERTS scheduler count. Existing selective correlation, unrelated mailbox retention, numeric/named insertion and cleanup/join stay unchanged. Runtime mismatch/halt/stop checks remain real. No new raw system_info tuple route is required for this first public increment; direct registration calls the existing internal Gather contract.

Source: erlang.erl13279–13352; native complete-list/ref validation erl_alloc.c3381–3424; known-disabled false is3271–3273. Native default eheap/binary success is outside the initial SDK profile. No blanket alwaysfalse or alwaysnotsup implementation is acceptable: the two sys_alloc public success paths above are mandatory.

## Small implementation map

Add Runtime types in separate files: AllocatorBackendCatalog (pinned names and immutable capabilities), AllocatorBackendCapability (state enum), AllocatorQueryKind (full/sizes), SystemAllocatorQueryProvider, SystemAllocatorQueryNames/Diagnostics, AllocatorQueryRegistrations. Keep semantic constants in their domain files. Modify CoreModules only to call the new registration group. Provider KnownAllocators comes from the recognition catalog; its producer checks support after request validation and builds only the truthful sys_alloc metadata above. Preserve private315 provider and all native ABI/C/collector/profile sources. No new package, native bridge, allocator-size syscall or global GC sampling is required.

Permanent public-registry HOST/contracts: both positive atom/list results; public unregistered→registered proof; actual7/zero/grow/free owner work around queries with live data preservation (metadata stays correct, no false size claim); existing private request counters remain independently dynamic; real actor mailbox unrelated-message retention and concurrent queries; halt/kill/drain reuse; full-list invalid-before-unsupported precedence; empty/duplicate/improper/wrong-outer/unknown; recognized unsupported notsup; no fake disabled entry. Preserve old ordered tests and derive registry deltas. After actual registration and passing validation, expected API presence is erlang349/351 and default507/optional509; these are future counts, not evidence of completed semantics.

Native pairable controls: exact sys_alloc sizes atom/list result, wrapper errors and request-order/unwrap behavior. Existing original19 covers many errors but **does not claim a sys_alloc full-info replay**; add a small reviewed original comparison case if needed. Compare full-info options key/order/e=true structurally while recording m=libc versus m=dotnet_native_memory as the explicit adaptation. Do not normalize that difference away or compare native byte totals/options/call counts to SDK NIF counters. Unknown-before-unsupported source precedence needs a separately labeled SDK adaptation assertion, not a fabricated native match.

## Real storage and memory/0,1 follow-up

Current reusable facts: BitString owns/takes byte[] and ByteView may share it (BitString.cs15–42); array payload capacity is real but wrappers are not unique storage. AtomNameRegistry retains global string identities/count, not bytes. Code images/resources have real leases, not JIT/native code byte ownership. Process mailboxes have lifecycle/counts but no process-heap partition. Native environments/tables and port HGlobal blocks are outside the joined payload ledger. Distribution AccountedBytes is an explicit queue tariff. None can honestly fill nine BEAM memory categories today.

After the public sys_alloc pilot, next measured storage increment is a shared BitString backing identity registered ONCE at allocate/take sites, preserving ByteView aliasing and defining process-global/host/cross-runtime scope. Count actual byte-array payload capacities only; label GC-observed liveness if using weak reachability. Deterministic runtime-owned accounting needs explicit storage ownership/leases. This is useful binary accounting work and must not block shipping the two truthful public metadata APIs. memory/0,1 stay absent until their adapted owned-storage partition and supported profile are defined; do not register always-notsup versions just for presence.

**Historical correction, without changing old evidence:** the312 audit demanded a coherent whole-runtime snapshot too strongly. Pinned erlang.erl12980–12982 explicitly says the summed memory amounts are NOT gathered atomically. No stop-the-world/global epoch framework is required for OTP fidelity. Preserve one computed record per memory/1 call so duplicate selections agree; each SDK source/domain must still produce internally valid observations and documented overlap rules. Native arithmetic total=processes+system, used<=allocated, nine-key order, capability-before-name-validation/notsup precedence and improper-tail behavior remain the target (13021–13088,13194–13272). CLR payload/accounting values are allowed to differ; invented ownership or GC.GetTotalMemory split into fake categories is not.
