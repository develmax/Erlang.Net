# erlang:load_nif/2: remaining whole-contract work

Source-only assessment of w536 commit3364c3050947c00084c19b446f98a05878b2512f against OTP29.1.1/ad058. No edits to source/ledgers, no build/native/VM/CI execution; PR96 CI and atom work were untouched. Exact input hashes are in source-hashes.json; all103 existing partition IDs are retained once in partitions-103.json. The180 callback declarations are a separate dependency inventory, not180 more MFA contracts/tests.

## Actual production progress versus stale baseline

The existing load-nif-batches.json/README still say entry admission is exact2.18 and genuine upgrade is missing. Both statements are obsolete. Current NifNativeEntryReader implements versioned prefixes/tails/legacy function layouts, and NifNativeImageInitializer executes genuine native upgrades with normal-progress barrier, previous image retention and close-aware resource/publication authority. Numeric714 and VM-owned Windows allocator repair independent-environment tuple creation across same-DLL generations. These are production changes, not merely more tests.

Finite evidence to preserve, not promote into whole closure:

| Evidence | What it establishes | What remains outside it |
|---|---|---|
| E1 Part543: each Debug/Release native84 + module-text8, linked final source | Versioned entry/options/error text and complete selected raw values/stacks | All103 partitions, all ABI/target combinations and callbacks |
| E2 Part544: each Debug/Release native-upgrade24 and affected controls | Selected genuine upgrade/private-data/veto/unload ordering; actual barrier/close mechanics | Resource-monitor/select, arbitrary callback dependencies and all source partitions |
| E3 env709: Release managed02 and Debug managed03-debug, each4 complete comparisons | One VM, one physical Windows DLL; actual allocator-pointer boolean, independent env, foreign OS thread and real cleanup | Two-VM DLL affinity, moduleless Linux namespace, generalized native-thread API/TLS |
| Exact current-head Linux/CI | PR96 running; no completed result asserted here | Must authenticate terminal mandatory jobs and final subject SHA before accepting |

The103-row source classification is:10 rows with a concrete direct gap,7 with an implemented slice blocked by a concrete dependency,85 existing/implemented protocols needing the remaining original matrix, and1 unresolved target-scope row. These counts are NOT passed tests or a percentage. None of the103 rows is marked fully accepted by this audit.

## Concrete source gaps, not requests for another indefinite test cycle

| ID | Exact remaining behavior | Current evidence / relevant partition IDs |
|---|---|---|
| D1 | Current caller while a separate pending on_load image exists must follow original image selection/order | ModuleRegistry.NifAdmission.cs95-96 explicitly rejects PendingCurrentAmbiguous; pinned erl_nif.c4764-4772 selects module.on_load after old-code check. caller.current-with-pending |
| D2 | Allocate an actual resource while its type is pending inside valid load/upgrade callback | NativeResourceType524.cs133-134 refuses pending && !registered. resources.allocate; affects type-create/takeover/load-time resource coverage |
| D3 | Genuine resource monitor/demonitor/DOWN and select/stop ownership protocol | Type524.cs135-136 rejects any Down callback; Resource527Registration installs11 operations and no monitor/select protocol. resources.down-monitor, resources.stop-select; affects takeover/purge-resource |
| D4 | Native continuation scheduling and mailbox send with actual rooted argv/receiver/image lifetime | enif_schedule_nif and enif_send absent from installed Windows callbacks/native bridge exports; pinned erl_nif.c3540 and860 provide real protocols. calls-env.schedule, calls-env.send |
| D5 | Missing lock/condition/rwlock, NIF thread API and system_info/dlopen families | Windows vector is zero-initialized and installs a subset; bridge exports do not implement these NIF families. A raw CreateThread/pthread scalar witness is useful but is not enif_thread_create or all native-thread operations. platform-callbacks.locks, sysinfo-dlopen; native-thread/all-callbacks/bootstrap dependencies |
| D6 | Independent env has no image; stable correct VM affinity across applicable native lifetimes | Windows same-VM allocator fixed. Linux Namespace.cs26 still captures runtime+first image and constructs new(runtime,image), unlike pinned erl_nif.c669-675 setup(NULL,NULL). Separate Windows VMs still share the physical DLL's copied global callback table. calls-env.independent-env; platform/native-thread dependencies |
| D7 | Required public per-process physical/offheap/GC/budget observables | retirement.public-metrics explicitly includes process_info/memory/GC. Current real native arena records are useful but do not implement the absent ordinary/default physical process fields. This depends on I-PHYSICAL; no CLR estimates or fabricated OTP counts |

Additional target boundary: PlatformLoader.cs11-13 admits only Windows/Linux x64. The existing scope row explicitly includes other architecture/OS/libc and says unsupported policy is not original compatibility. Enumerate the project's REQUIRED advertised targets explicitly; do not silently treat an implementation guard as a newly approved scope reduction. If wider targets are required, their actual ABI/platform implementations are a production gap. No failure was executed on an unadvertised target here.

Source absence of a valid callback is material to load_nif itself: a genuine valid library can use a lock/resource/monitor during its load callback. Passing a zero-function entry or scalar native body does not remove that dependency. Conversely, the103 rows must not be inflated by adding every callback name as a separately closed MFA.

## Finite whole-closure gates

| Gate | Bounded deliverable and done condition |
|---|---|
| G0 Target/profile/backend list | Predeclare required targets (at least actual Windows x64 and glibc Linux x64), ordinary/reviewed loader routes, default/private-owned runtime profiles, actual source/BEAM positive backends, and applicable legacy/reallocation deployment policies. Host/foreign-context calls are negative authority controls, not fabricated positive Erlang callers. Record any wider required OS/ABI targets rather than leaving an implicit universal claim |
| G1 Finish the current increment | Authenticate current exact-head required Linux CI, native84/module8, N2 upgrade24, env4, callback68/pthread/ordinary-loader3 and cleanup as finite scopes. Replace stale N1/N2 missing-implementation descriptions; do not flip whole-MFA status from them |
| G2 Resource production tranche | Implement D2+D3 together: valid callback-time allocation, monitor identity/DOWN, select registration/stop/cancel and type takeover. Done when actual original/current callback event streams and resource/image completion agree for create/takeover/veto/process death/close. No pending-pointer-only implementation |
| G3 Native call/thread production tranche | Implement D4+D5 in coherent families, including actual off-turn env/thread affinity and dirty scheduling. Bind each pinned callback declaration to real implementation or an explicitly justified applicable-version exclusion; remove callable null-slot gaps for required libraries. Isolated genuine loader/callback fixtures must prove effects and cleanup |
| G4 Caller/env production tranche | Fix D1 and D6 with actual original authority/VM semantics. Required cases include current+pending, pending caller, old caller precedence, Linux moduleless env and same-DLL valid/veto/retirement. Preserve existing negative host/foreign/stale cases |
| G5 Metric/target dependencies | Complete D7 through the real I-PHYSICAL owner; implement wider required target/ABI gaps from G0. Do not label these evidence-only work when source still refuses valid original behavior |
| G6 Close the existing103 rows on final composition | For EACH existing row assign finite source-branch/type/state witnesses and all applicable G0 compositions before running. Reuse existing path/dependency/version/resource suites; both routes collect independent failures; full returned errors or raised complete stacks and actual lifecycle effects required. Path/OS-specific original strings are compared on their own target, not substituted from Windows. Thermal/time-dependent values require source-defined invariants, not fabricated literal equality |
| G7 Whole-MFA decision | No direct gap, missing dependency, unassigned witness, unexplained mismatch, skipped required composition or cleanup failure remains; current exact-head mandatory CI/reviews green. Then load_nif may change known24->23. API registration delta remains0; the504 pending MFAs do not disappear |

The current finite source partitions make the acceptance boundary explicit; they do not guarantee absence of future bugs. Every newly discovered source branch must be assigned deliberately, with its original evidence, rather than extending an unbounded retry loop.

## Priority recommendation

load_nif/2 is NOT the nearest whole API closure simply because it occupies one ledger row. Its remaining dependency burden includes native resource monitor/select, several callback families, VM affinity and I-PHYSICAL. Finishing another scalar upgrade example would add finite evidence without removing those gaps. The next useful implementation tranche here is G2 (resource allocate/monitor/select together), while a separate bounded selector/report API can be considered for the first whole-MFA reduction by the agents assessing the other23. This agent does not select or alter their statuses. No ETA is justified by the available measurements.
