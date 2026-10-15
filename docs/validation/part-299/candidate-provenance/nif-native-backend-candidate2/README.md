# Internal trusted NIF backend candidate 2

SOURCE ONLY. This overlay has not been built, formatted, loaded, natively executed or compared with fresh original OTP observations by its author. It is not promoted. Base: published core296, 1d98ead. No existing Main file is replaced; all overlay files are new. No public load_nif BIF or additional source/BEAM dispatch is enabled.

## Concrete increment

NifNativeImageInitializer.InitializeAsync is intended as the existing NifImageLoadReservation initializer delegate. It returns NifImageInitialized containing the actual library owner and bound ErlangFunctions. The reservation retains registry permission and exact-image lease; this backend does not reacquire them or substitute the old fixture generation table. Cleanup before returning belongs to the initializer; success transfers cleanup to the reservation/image.

The profile is explicitly the unchanged reviewed two-function fixture source SHA256 7B6AF591C635DED2FE686A9E8671ACD58F9C8A6A080D47C61420F844FF33F074 plus an independently trusted binary hash. A supplied hash does not prove source-to-binary provenance. Deployment/build evidence remains necessary. No arbitrary library can safely use this partial API merely because imports appear compatible.

Windows x64 uses the pinned 180+2 callback layout with ten populated fixed signatures: the seven reviewed fixture functions and three exception introspection/raise functions. All remaining slots are null. No fake fixed-signature variadic callbacks are installed. Linux x64 glibc uses the same ten fixed operations plus actual C variadic tuple/list wrappers, twelve enif exports in total. This is a bounded profile, not ABI 2.18 completeness. Native initialization is allowed only for the trusted fixture profile; the extra callbacks have managed and isolated ABI probe source, not new arbitrary-library admission.

The native owner serializes synchronous native frames and unload on its private gate. Actual NifImageCall ownership remains the dispatch layer's responsibility. Bound calls reject a different ProcessRuntime, but full registry/runtime shutdown association and physical native resources are unfinished. This increment does not solve native-created threads, dirty work, binaries, resources, upgrade, halt/unload-thread hooks or full platform interoperability.

## Corrected semantics and ownership

Guest pending reason is latest-wins and retained independently from host ExceptionDispatchInfo. Native raise preserves the exact supplied reason handle for has_pending_exception; it does not manufacture an equal replacement handle. Pending guest failure wins over an ordinary native return value. is_exception requires the sentinel and pending flag. A simultaneous host error produces NifNativeHostFailureException containing both the original host EDI and guest reason after environment disposal. Every unmanaged callback catches managed failures and returns a bounded ABI sentinel; exceptions are raised only after the native frame returns. Catastrophic runtime failure and invalid native memory are not recoverable by catch clauses.

The modern entry is read only after its two-int version prefix confirms this exact profile's 2.18. Older layouts are refused without reading modern optional fields. Native flags/options/reload/upgrade and non-allowlisted functions are refused before load. The exact reserved descriptor must declare the fixture's NIF functions.

A successful load marks initialized ownership before managed pending/cancellation checks, so later failure unloads once. A load veto closes/reclaims without normal unload. Failures from cleanup and close are retained rather than replacing the primary initialization error. After native unload, the tracked allocation count is captured before fallback reclamation; the fixture harness checks it is zero. Containment fallback is not claimed as general native resource parity.

ModuleRegistry.NifTaints adds a lazy ordered/deduplicated persistent metadata set, marked after successful OS open and nif_init lookup, immediately before dynamic nif_init. Entry rejection/load veto remains tainted; open/symbol failure does not. It uses only metadata gate, not a new code permission. GetNifTaints is internal; no taints BIF is exposed.

Linux maps the already hashed file via /proc/self/fd, retaining its descriptor for the entire image/shim lifetime. This prevents pathname replacement and descriptor-name reuse colliding with dlopen's loaded-name cache. Relative private dependency resolution is outside this exact fixture profile. The process-global bridge registration validates version/size and every required pointer, publishes once and refuses replacement. Its shim, callback table and verified descriptor remain process-owned. A collectible managed provider is rejected; symbol interposition is checked against RTLD_DEFAULT. The bridge is not unloaded while global imported addresses could survive.

## Minimal C exception

work.md section 2.1 permits unavoidable platform interoperability. The three implementation files native/nif_bridge_api.h, nif_bridge_exports.c and nif_bridge_varargs.c are only signature forwarding, one-time pointer publication and actual C va_start/va_arg/va_end ownership. C# owns every Erlang term, environment, exception, allocation policy and construction algorithm. A fourth C file, nif_bridge_abi_probe.c, is test infrastructure exercising 17 arguments crossing register/stack boundaries, zero arguments, repeated pending raises and ignored ordinary return. No format/resource/runtime algorithm is in C.

Pinned OTP headers remain external build inputs, not copied or stripped here. Their hashes/license provenance and the documented exception are in ../nif-native-backend-design1. No LICENSE file or native binary is created. NativeAOT remains an optional fixed-export experiment: not built here, not a proven complete substitute for genuine C variadic access.

## Harness and future execution

The tool project is Erlang.NifNativeBackend; a separate new NifNativeHostFriend.cs grants only this development assembly internals access. It does not change existing friend attributes. No-argument invocation checks latest pending reason, host/guest separation, exact raw reason handle, ignored ordinary return, null pending output, expired env, nested failure scope isolation, modern prefix refusal, table size and persistent taint snapshot/order/dedup. These are authored source checks, not passed checks.

Two arguments run the existing trusted fixture binary on Windows. Four arguments additionally identify the Linux bridge. Six additionally identify the isolated ABI probe. Hash arguments must come from independently verified build evidence, not a self-approved arbitrary downloaded binary. All tool output distinguishes managed checks, bounded native fixture checks and ABI probe checks; it never prints an original OTP pass.

Future commands are proposals in validation-plan.json. Root must copy overlay to its isolated candidate, review and build serially when resources permit. Existing Part290 original fixture observations establish only their original seven semantics. New pending/variadic behaviors require new original observations with the independent expected policy authored in original-expected-policy.json before collection. Native ABI probe output alone is not differential verification.

## Remaining blockers

No full callback set (180 Windows/176 Linux); no all-format va_list, set_option lifecycle, resource/binary ownership, native threading, dirty scheduling, native async sends, general compatible older ABI reads, upgrade or arbitrary entry safety. Windows variadics remain absent. Runtime registry association, shutdown and full source/BEAM route integration require the parent's separate lifetime work. Exact cancellation-versus-pending precedence is unproved. Process-global shim retention is deliberate; image-owned libraries unload separately. The four C files are reviewed source only, not executable artifacts.

## Candidate 2 peer-review corrections

Candidate 1 remains immutable. Read-only lifecycle review found that native allocation succeeded before HashSet insertion could fail; candidate 2 frees that untracked pointer on insertion failure. Teardown now attempts reclamation, OS close, verified descriptor disposal and Windows callback disposal independently, aggregating failures so file disposal cannot skip remaining cleanup or mask the earlier native failure. Native callback host recording now assigns only the caught Exception reference; EDI capture happens on the managed continuation after native return, avoiding capture allocation in the callback catch. These are source corrections only, not injected OOM/cleanup test results.
