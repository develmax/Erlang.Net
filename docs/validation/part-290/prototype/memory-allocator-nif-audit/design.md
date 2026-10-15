# Runnable NIF foundation: source-only proposal

No Main edit, build, installation, native execution, network use or API-presence claim. Original bindings and numbered extracts are in source-bindings.json. All ABI sizes below are source-derived x64 predictions, requiring a native offsetof/sizeof fixture before adoption.

## Compiler and module identity

Current Execution.Evaluate handles Expr.Call by evaluating arguments, checking local source functions, then tail optimizations and Modules.Call. Direct load_nif must be intercepted **after local-function resolution but before tail-call rewriting**. Only syntactically direct erlang:load_nif/2 (or a resolved imported equivalent) receives an unforgeable current-code-image capability. Do not infer a direct call from a matching module/function at generic dispatch: apply, fun references and a wrapper must retain the dynamic BIF rejection. Source semantics for constant apply need original compiler disassembly, rather than assuming the syntax classification survives optimization.

ModuleDefinition.Register already publishes a ModuleCodeDescriptor with SourceOwner=this. ProcessContext.ExecutingSourceImage(owner), source leases and AsyncLocal source scopes identify the generation currently executing. ModuleRegistry.CurrentCodeImage(name) alone is insufficient: old code may be executing. ModuleRegistry has current and old image maps, purge state, and pending on_load tickets. The prepared BEAM interpreter and source compiler both need direct-call handling; implementing only source AST calls leaves bytecode load_nif unsupported.

Proposed entry: LoadNifDirect(ProcessContext context, ModuleCodeImage callerImage, object callerOwner, Term filename, Term loadInfo). Validate that image/owner is the current active scope; determine current/old/pending on_load generation under registry gate. Pin a source lease and a pending-load reservation by **image identity**. Convert filename before old-generation error, matching erl_nif.c4708-4764. Reject generic dispatch with pinned bad_lib tuple; do not validate its filename first.

Never hold the registry gate while opening libraries or running init/load/upgrade/unload/NIF callbacks: callbacks can reenter host services. Capture generation/reservation, release gate, run native work, reacquire and revalidate exact generation, then atomically install function overrides. If publication lost the race, unwind the newly loaded instance once and preserve replacement code. Keep original bodies available for load failure/on_load rollback. Bind overrides to image, not a single mutable module-wide function dictionary: old local code and new exports can coexist. Source local InvokeAsync must consult image-bound NIF overrides, as well as external Modules.Call. Closures and BEAM local calls require the same rule.

For on_load, the NIF instance belongs to ticket.CodeImage even before publication. Existing FinishPendingOnLoad already compares captured current/old descriptor/bodies and waits for callback quiescence. Extend pending cleanup to unload unpublished NIFs exactly once. Successful publication transfers ownership; deleted/old/purged images retain native leases until every active native call and resource callback drains. Never free a library merely on DeleteModule while a NIF fptr is executing. Cross-image upgrade and resource takeover remain subsequent contracts, not silently emulated reload.

## Exact pinned ABI

OTP pin has ERL_NIF_MAJOR_VERSION=2, minor=18, VM variant beam.vanilla. ERL_NIF_TERM is ErlNapiUInt, pointer-sized on Windows x64 and Linux x64: use nuint, not C# ulong as a universal platform assumption. C int/unsigned are 32-bit; size_t is nuint. C long is 32-bit Windows LLP64 and 64-bit Linux LP64. ErlNifEnv* is opaque nint; a native function is `delegate* unmanaged[Cdecl]<nint,int,nuint*,nuint>`.

ErlNifFunc predicted offsets: name0:nint, arity8:uint, fptr16:nint, flags24:uint; size32/alignment8. ErlNifEntry: major0:int, minor4:int, name8:nint, num_of_funcs16:int, funcs24:nint, load32:nint, reload40:nint, upgrade48:nint, unload56:nint, vm_variant64:nint, options72:uint, sizeof_ErlNifResourceTypeInit80:nuint, min_erts88:nint; size96/alignment8. Do not marshal the native strings as managed-owned memory. Copy metadata while library is leased; bounds-check function count and signatures before publication.

Load/reload: `int(ErlNifEnv*, void**, ERL_NIF_TERM)`; upgrade: `int(ErlNifEnv*,void**,void**,ERL_NIF_TERM)`; unload: `void(ErlNifEnv*,void*)`. Unload has no return term. ErlNifBinary has size0:nuint, data8:nint, ref_bin16:nint, spare24/32:nint; size40. Binary/resource ABI is outside the first fixture.

Windows dynamic init is `ErlNifEntry* nif_init(TWinDynNifCallbacks*)`. The header copies the entire callbacks struct into each DLL. The source-derived Windows table is **180 function slots plus two trailing pointers, 1456 bytes** on x64. Preserve all slots and exact declaration order, including four int64 slots under SIZEOF_LONG != 8. The generated windows-callback-layout.json records every ordinal/signature; do not sort it. The seven fixture slots are priv_data0, alloc1, free2, get_int10, make_badarg18, make_int19, make_string27. Unsupported slots must not accidentally point at a callback with the wrong ABI. Null slots only constitute a restricted fixture host; arbitrary NIFs that call them are unsupported and cannot be advertised safe. Full support needs typed implementations and varargs handling.

Linux dynamic init is `ErlNifEntry* nif_init(void)`. Existing NIF shared libraries have unresolved enif_* symbols supplied by the process's global loader namespace. NativeLibrary.Load does not export JIT UnmanagedCallersOnly methods as ELF symbols. A global export shim is necessary for unmodified native NIF binaries.

## Preferred C# NativeAOT export-forwarder shim

Prefer an owned C# NativeAOT shared library containing **no Erlang.Terms references, managed Term objects or ownership ledger**. It exports fixed-signature enif_* forwarders using `[UnmanagedCallersOnly(EntryPoint="enif_get_int", CallConvs=[typeof(CallConvCdecl)])]`. Each forwarder calls an immutable `delegate* unmanaged[Cdecl]` callback supplied by the main JIT host. Env pointers and term handles remain opaque integer/pointer values; the main JIT runtime owns all term state. Both .NET runtimes stay alive while function pointers are reachable.

An exported shim initializer accepts ABI version, table byte length and host callback pointers and rejects incompatible or second incompatible initialization before any NIF loads. Store only unmanaged function pointers in shim state; publish atomically. Process-global no-env callbacks (alloc/free) must be stable across ProcessRuntime instances; env-bound callbacks resolve runtime/instance from the JIT-owned environment. Windows can use main-host callback pointers directly and needs no symbol-export shim for this fixture.

Load Linux shim with explicit dlopen(RTLD_NOW|RTLD_GLOBAL) through a tiny C# P/Invoke wrapper, then load the fixture. Retain the shim process-wide until all NIF handles are gone. NativeLibrary.Load alone does not promise RTLD_GLOBAL. Platform loader flags and libdl naming require actual target verification. Linker symbol export/visibility and generated .so dependencies must be checked with nm/readelf; no such verification has run here.

**Varargs is a real boundary.** UCO methods cannot implement arbitrary C varargs such as enif_fprintf, enif_snprintf, enif_make_tuple or enif_make_list. Fixed fixture calls avoid them. Full ABI needs a narrow native varargs adapter or an independently validated architecture-specific solution; do not cast fixed signatures onto varargs. A C helper is justified only for that explicit ABI boundary after the C# forwarder option is tested, not for a second term runtime. Linux LP64 aliases enif_get_int64/make_int64 to long variants; Windows has distinct table slots.

## Local toolchain audit, no installation

Installed dotnet lists SDK6.0.428 and pinned10.0.400. SDK targets contain a conditional ILCompilerTargetsPath import when PublishAot=true. No microsoft.dotnet.ilcompiler or runtime.win-x64.microsoft.dotnet.ilcompiler directory was found in the inspected user NuGet cache, and installed packs list ordinary host packs rather than an AOT compiler pack. Therefore NativeAOT publication is **not confirmed available offline**. Visual Studio18 Community with VC.Tools.x86.x64 was found by vswhere, although cl/link/clang were not in the current PATH. Linux toolchain availability was not inspected.

The installed dotnet/LICENSE.txt begins MICROSOFT .NET LIBRARY license terms; it is not sufficient evidence for the exact NativeAOT runtime/ILCompiler package redistribution license. Before adoption inspect the actual pinned ILCompiler/runtime pack nuspec, LICENSE and third-party notices, record package hashes and transitive native runtime dependencies. No pack was fetched, license conclusion made or project dependency added by this audit. NativeAOT is a proposal pending restore/toolchain/license and export proof, not an installed capability.

## Smallest real fixture and seven callbacks

fixture.c is a normal pinned-header ERL_NIF_INIT library for memory_allocator_nif_fixture. It genuinely executes native increment/1 and greeting/0. load accepts integer42, allocates private state and vetoes other input; increment extracts a signed32 integer, rejects INT_MAX/noninteger with badarg, updates native private state and returns value+1; greeting returns a Latin1 Erlang string. unload frees state and increments an independently observable native counter. The exported unload counter is inspected **after unload callback and before releasing the OS handle**, never after NativeLibrary.Free. First fixture is deliberately single-threaded; the counter is not a claimed concurrency implementation.

Required JIT UCO signatures:

| API | Signature |
| --- | --- |
| enif_priv_data | nint(nint env) |
| enif_alloc | nint(nuint size) |
| enif_free | void(nint pointer) |
| enif_get_int | int(nint env, nuint term, int* output) |
| enif_make_badarg | nuint(nint env) |
| enif_make_int | nuint(nint env, int value) |
| enif_make_string | nuint(nint env, byte* zeroTerminated, int encoding) |

Main host creates a call-scoped environment with owned GCHandle/state and a numeric handle table retaining input/output Terms. Callbacks resolve only through that environment. Never expose a managed object address or GCHandle as a BEAM-tagged term. Opaque handles work for this seven-function subset because fixture never inspects term bits; this does not implement header macros that directly access PID fields, resource/binary representations or environment-independent identity callbacks. Handles must remain unique/valid for the complete active call; reject unknown handles and keep roots alive until native return. No managed exception may escape a UCO callback: catch, store a pending exception in the environment, return the appropriate failure/sentinel, then raise on managed return. enif_make_badarg stores a guest badarg exception; its result must not be treated as a normal returned value. enif_alloc/free use matching NativeMemory allocation and an actual owned allocation registry, not managed arrays handed to free.

## Ten proposed code areas and gates

1. Compiler NifCallSyntax plus Expr.Call interception and source declaration validation; preserve imported/local resolution and dynamic apply.
2. ModuleCodeDescriptor declared NIF signatures and module_info(nifs) based on actual declaration metadata.
3. ModuleRegistry.Nifs image-bound reservations/install/rollback/dispatch; integrate local source, external and BEAM entry paths.
4. NifLibraryLease owned native handle and active-call/unload accounting; hooks in purge/on_load retirement.
5. NifAbi structs plus platform init signatures and callback table generator from pinned header.
6. NifCallEnvironment handles/rooting/pending exception and callback lifetime.
7. NifPrimitiveCallbacks seven actual fixed APIs with separate semantic constants.
8. C# NativeAOT Erlang.NifExports project and Linux global-loader wrapper, subject to audited pack/license availability.
9. Native fixture build/sizeof/offsetof/export scripts plus managed HOST tests; each namespace-level type in its own C# file.
10. Original standalone observation collector: same C fixture source/header pin, direct and dynamic calls, full errors, load veto/reload, code-generation/on_load cleanup and unload event provenance.

Acceptance before public-complete claim: compile the same native fixture against exact pinned headers on Windows/Linux, assert native layout/table offsets; load through real entry; increment0->1/-1->0/INT_MAX-1->INT_MAX; exact badarg for INT_MAX/noninteger; actual greeting "native hello"; load veto leaves original functions intact and frees tentative resources; successful load/reload rejection; dynamic apply never installs; old-image and failed on_load preserve active generation; in-flight native calls defer unload; counter exactly once and native memory registry empty. Retain original complete raw outputs separately from SDK observations. These establish a genuine bounded NIF subset. Resource callbacks, async/dirty jobs, native threads, full ABI macros, upgrades and arbitrary existing NIF libraries remain required later work for whole-goal completion.
