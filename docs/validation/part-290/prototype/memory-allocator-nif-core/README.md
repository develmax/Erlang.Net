# NIF internal prerequisite prototype

Source-only artifact. **Not built or executed.** No public erlang registration, Main edits, native dispatch, package restore or installation. This must not change the API completion count.

The executable project references existing src/Erlang.Runtime. It implements actual opaque native environment addresses backed by JIT-owned dictionaries, environment-local Term handles, seven fixed-signature native callbacks, native allocation ownership, image/caller validation and once-only retirement after in-flight leases drain. WindowsNifFixtureLibrary invokes the native pinned-header nif_init/load/functions/unload pointers; it does not replace execution with canned results.

Native environment storage contains no managed references. Native ABI receives opaque integers and addresses only. Strong managed roots remain in the main JIT dictionary until the call returns. No GCHandle/object address is used as a native Term. Unknown handles, callback exceptions and badarg remain pending managed failures and never unwind across the unmanaged boundary.

Environment addresses come from NativeMemory and can be reused after disposal. A native callback that illegally retains an expired ErlNifEnv pointer could therefore address a later environment at the same address; this prototype does not detect that stale-pointer/address-reuse case. Only synchronous callbacks during the fixture's active call are supported. Stale callbacks and callbacks from native-created threads are explicitly unsupported. Globally unique Term handles prevent cross-environment Term-handle reuse, but do **not** solve stale environment-address reuse. No protection against arbitrary native use-after-free is claimed.

Retired generation state is removed from the strong dictionary as soon as its active count reaches zero, before calling unload outside the gate. Repeat retirement is a no-op. A ConditionalWeakTable retains only weak image keys and an owner-free marker to refuse rebinding a retired live image; it never retains the unload delegate, caller, native instance or image strongly. Missing-generation acquisition reports ObjectDisposedException. The prepared harness asserts zero retained strong generation entries after release and repeat retirement.

The optional native path intentionally admits only a trusted exact artifact hash plus the expected pinned two-function fixture entry. The trusted hash must come from a separately reviewed fixture build receipt. Never compute it from an arbitrary input library and call that an allowlist. Unused callback slots are null, with the **explicit limitation that arbitrary native libraries are unsupported**. This is not a safe generic NIF loader. Only the fixed reviewed fixture, whose seven used APIs are implemented, may be loaded. The string constructor is bounded to 4095 Latin1 bytes for this fixture.

## Deferred execution commands

Not executed by this agent. Root may grant a bounded build after its current gate finishes:

```powershell
dotnet run --project artifacts/memory-allocator-nif-core/NifCore.csproj
dotnet run --project artifacts/memory-allocator-nif-core/NifCore.csproj -- <absolute-reviewed-fixture.dll> <trusted-build-receipt-sha256>
```

No-argument mode exercises managed ownership and actual UCO function pointers but explicitly reports that native fixture execution did not occur. Two-argument mode uses a real Windows x64 DLL compiled from ../memory-allocator-nif-audit/fixture.c against pinned generated OTP headers. Native build and native sizeof/offsetof validation remain unexecuted. Fixture SHA256 is platform/build-specific and cannot be supplied until that independent build exists. Linux and NativeAOT forwarders are not implemented.

## Harness coverage prepared, not proven

- Environment isolation; integer extraction/construction through actual Cdecl UCO pointers; wrong integer type; pending guest badarg; native allocation/free ownership; no live environment after disposal.
- Current caller image ownership; foreign owner refusal; retire while leased defers unload; retired acquisition fails; repeated release/retire unloads once.
- Optional actual native increment boundaries, wrong-type badarg, greeting string; load-info veto; native unload counter queried before OS handle release; outstanding native allocation count recorded before fallback reclamation, so fallback cleanup cannot conceal a native leak.

## Exact unimplemented integration

1. Direct source compiler lowering versus dynamic apply and compiled BEAM load_nif instruction handling.
2. Parsed -nifs declarations, signature validation and module_info(nifs).
3. Generation-bound function override installation in source local calls, closures, external calls and BEAM execution; original-body rollback.
4. Atomic registry reservation/revalidation across native initialization. Current standalone table checks current image, but has no registry transaction against simultaneous replacement/purge. It does not claim that integration race solved.
5. Pending on_load image admission and commit/retirement transfer. Current bootstrap only accepts current images, so on_load is intentionally unimplemented.
6. Registry purge/delete hooks and active code/resource lifetime integration. The standalone table can defer its own in-flight native calls; it does not yet observe Main purge.
7. Linux exported enif symbols, NativeAOT shim build/initialization, global loader namespace and exact pack/license verification.
8. General NIF ABI, resource/binary macros, variadic APIs, native threads, dirty jobs, reload/upgrade/resource takeover and concurrent callbacks. The fixture's native private counter and allocation owner are serialized by the native-call gate; do not extend to native threads without synchronization.
9. Full native entry-version/platform error contracts and filename semantics. This bootstrap intentionally restricts the exact fixture; NotSupportedException here is internal prototype failure, never a substituted guest load_nif result.
10. Independent original execution, native layout/export checks, compile/style gate, cancellation/race interleavings and all native failure rollback tests. None has run.

Further integration must keep callbacks outside registry locks and preserve owned cleanup after caller cancellation. Cancellation is checked before lease admission and native calls, then after native return; synchronous native instructions cannot be forcibly interrupted safely. Unload ignores caller cancellation to release owned native state. Fallback reclamation frees host-owned allocations after failure but is reported separately from the native unload's observed outstanding count.
