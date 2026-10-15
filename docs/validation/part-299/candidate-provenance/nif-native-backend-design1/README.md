# Native NIF backend: bounded implementation plan

Status: source audit and design only. No native backend, ABI-complete support, load_nif BIF, BEAM dispatch or native execution is claimed by this artifact.

## Decision

Implement environment, term, exception, allocation, image ownership and native-library lifecycle in C#. Use a minimal platform ABI veneer for Linux named/global exports and genuine C variadic/va_list access. The veneer must not implement Erlang terms, formatting policy, exceptions, scheduling, resource lifetime or registry admission. The documented exception is permitted by work.md section 2.1. Do not fork OTP's runtime in C.

The first executable increment should be an internal, explicitly bounded trusted-library capability profile for the existing seven-callback fixture, plus the tuple/list variadic bridge as a separate ABI test. It is not safe to present a callback table with unsupported slots as a general NIF loader: nif_init executes before metadata validation and can call arbitrary API entries. Rejecting dirty flags or unsupported metadata afterward does not repair this gap. Loading arbitrary libraries remains disabled until required ABI coverage and ownership semantics exist.

## Evidence and platform split

All OTP references are bound to OTP-29.1.1 commit ad05823719d77c8faee87348ea39513d4e2f99c5, not the newer checkout HEAD. source-bindings.json records raw source hashes. abi-inventory.json records each declaration and Windows ordinal. There are 180 Windows callback slots plus two internal tails; Linux declares 176 API exports because four int64 APIs alias long APIs when sizeof(long)==8. Five APIs are C-variadic and two consume va_list.

Windows nif_init receives the exact callback structure and copies it. Linux nif_init takes no arguments; the original fixture ELF has seven unresolved enif_* symbols. OTP opens NIFs with RTLD_NOW|RTLD_LOCAL after the emulator already supplies public symbols. CoreCLR UnmanagedCallersOnly pointers do not by themselves create named/global ELF symbols. Register a process-owned veneer with RTLD_NOW|RTLD_GLOBAL before opening a library locally; resolve the platform loader deliberately, initially Linux x64 glibc only. Do not claim musl/ARM or Windows ARM support from x64 layouts.

C int/unsigned are 32 bits, size_t and ERL_NIF_TERM are pointer-sized. C long is 32-bit on Windows x64 and 64-bit on Linux x64. ErlNifEntry modern x64 size is 96; ErlNifFunc size 32; ErlNifBinary size 40. Read only a version-safe prefix before interpreting optional fields: exact modern marshaling of an older shorter entry can read beyond allocation. The bounded fixture profile may require 2.18 exactly; compatible older-version support requires explicit layouts and tests.

## Runtime ownership contract

NifNativeImageInitializer.InitializeAsync receives the reserved exact code image and trusted capability profile, opens and validates a library, invokes load, creates immutable name/arity bindings, then returns NifImageInitialized(owner, bindings). Before return it owns all cleanup. After return the existing reservation owns cleanup until publication transfers it to the image. It must not reacquire the registry modification permission already retained by the reservation, call its own disposal or treat AsyncLocal diagnostic identity as admission authority.

Each binding is an ErlangFunction delegate capturing its library owner and native function pointer. The existing image call admission owns the code image and prevents owner retirement during calls; do not graft the standalone fixture generation table onto Runtime. Each native call creates a scoped environment containing the caller ProcessContext, owner private data, owned term handles and separate guest/host failure state. Native pointer, environment and returned-term validation occurs before managed consumption. The owner cannot free its OS handle until image calls, initialization callbacks, cleanup callbacks and supported native activities have ended. Native-created threads/resources/dirty jobs remain unsupported until explicit ownership joins exist.

Load callback return zero means successful initialization. A nonzero veto rolls back resources and closes the OS handle; do not call ordinary unload as if the library had been published. A successful load followed by managed failure is an initialized owner requiring exactly-once cleanup. Library close and unload errors must not erase the primary failure. Never silently report containment reclamation as native ownership parity.

## Exception boundary

No managed exception may unwind through a native frame. Each unmanaged entry catches managed failures, records ExceptionDispatchInfo as host failure and returns an ABI-valid sentinel. GuestPendingReason is separate and overwriteable: OTP enif_raise_exception replaces the reason, and enif_make_badarg raises badarg. An ignored sentinel followed by an ordinary returned term still throws the pending guest reason. enif_is_exception requires both pending state and sentinel; enif_has_pending_exception reports the flag and writes the latest reason only when its output pointer is nonnull. Do not reuse the fixture's first-exception-wins Fail state for these APIs.

After the native frame returns, the managed continuation first handles host failure as a host diagnostic with any guest pending reason preserved separately, then pending guest exception, then normal returned handle validation. Cancellation/halt versus native guest exception precedence is not established by this design and needs targeted original observations. Allocator failure and callbacks without a recoverable environment need an explicit per-native-call failure frame; arbitrary native thread entry remains unsupported.

## Veneer shape

Use an immutable process-wide typed callback registration, ABI version, structure size and complete required-slot validation before publishing any pointer. Once published, pointers and the owning managed provider/ALC stay pinned for process lifetime; a second incompatible registration fails. NIF libraries remain image-owned and unloadable, but the global export shim is not unloaded while any imported address could survive. Registration occurs before consumers, so no racy live callback replacement is required.

Fixed-signature exports are mechanical forwarders into registered C# UnmanagedCallersOnly callbacks. Windows can bind these managed pointers directly except actual variadic slots. Preserve exact header ordering. A partial bridge exports only its declared capability profile and is never ABI-complete.

For tuple/list varargs the C entry owns va_start/va_end; managed code receives count plus a stack-scoped opaque cursor and obtains ERL_NIF_TERM values through a typed va_arg reader. C# constructs and validates the tuple/list. This avoids native semantic allocation and a fake all-pointer ABI. Cursor readers cannot retain a cursor or escape into asynchronous work. Counts are validated in C# before iteration.

For fprintf/snprintf and their v variants, C owns va_copy/va_end and typed cursor readers for promoted int/unsigned, long/unsigned long, long long/unsigned long long, double, pointer and term. C# owns format parsing including star width/precision, length modifiers and %T. FILE* output may require a narrow fwrite forwarding primitive. Truncation, terminator and return-count behavior need separate tests; no formatter is supplied by this first bridge.

For set_option the veneer decodes only the enum-dependent ABI argument: delay_halt consumes none, on_halt and on_unload_thread consume callback pointers, unknown consumes none. C# validates loading state, duplication and lifecycle and supplies the native-compatible errno. These options stay unavailable until their lifecycle is implemented.

## NativeAOT alternative

Installed SDK targets support PublishAot and UnmanagedCallersOnly.EntryPoint documents named exports; neither proves a working Linux shared export bridge here. Default local NuGet cache has no inspected ILCompiler package; restore/publish, ELF export inspection, RTLD_GLOBAL loading, Windows table ABI and process teardown remain unperformed. A NativeAOT forwarder must hold only unmanaged callback pointers, not share Runtime types/GCHandles between CoreCLR and its separate runtime. It still requires genuine variadic ABI access. Its additional runtime startup, memory and packaging cost is unmeasured. Keep it as an isolated experiment rather than a prerequisite that indefinitely blocks the minimal interoperability veneer.

## Validation gates before promotion

1. Source/layout: generated ABI inventory matches pin, all profile slots nonnull, negative version/size/profile cases fail before consumer init. Compare Windows/Linux widths and modern version-aware entry reads. Do not dereference intentionally invalid native pointers in ordinary host tests.
2. Pure host: returned handle identity and expiration, latest pending reason, ignored exception sentinel, host failure separation, native callback reentry guards, load veto versus initialized failure, exactly-once unload and cleanup failure retention.
3. Native ABI: compile only in isolated CI; prove fixed exports by nm/readelf, direct Windows callback ordinals, tuple/list zero/one/many args and repeated va_copy traversal, variadic register/stack spill, long width, null output pointer, fixture calls and unload marker before handle close. Retain command/exit/output hashes.
4. Original OTP comparison: author expected typed policy from the pin before collecting raw ETF, run fresh original VMs for pending exception behavior and fixture lifecycle, retain raw observations. Collection success is not a semantic pass. Never reuse prior seven observations as proof of new exception/varargs behaviors.
5. Integrated lifetime: exact caller image, current/old/pending denial, reload/upgrade refusal, purge during admitted native call, cancellation during init, native cleanup callback faults, source local/tail/closure and BEAM routes. Existing core296 host barriers are prerequisites, not proof of native ABI behavior.
6. General ABI expansion: resources, binaries, maps, scheduling, native threads, callbacks, takeover, upgrade and shutdown require independent implementations and lifetime tests. No public load_nif until all reachable entry routes and supported ABI lifetimes are accounted for.

## Deliberate blockers

The seven fixture functions are not a general API subset discoverable from imports: libraries can call function pointers conditionally. Capability profiles therefore require trusted source/hash or equivalent deployment contract. Resource callbacks, native threads, enif_send, dirty scheduling, upgrade and halt callbacks are not covered. ABI-safe return values for every unsupported function cannot be invented. Public arbitrary native code can crash the process regardless of managed checks; tests must isolate intentional malformed native behavior in child processes.
