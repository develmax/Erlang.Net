# Internal trusted native fixture backend

This development tool exercises the real Runtime image initializer, bindings and library owner. It does not register `erlang:load_nif/2` and does not implement the complete NIF ABI.

The admitted profile is the reviewed two-function fixture source SHA256 `7B6AF591C635DED2FE686A9E8671ACD58F9C8A6A080D47C61420F844FF33F074`, together with independently verified build provenance and its binary SHA256. A caller-supplied hash alone does not establish that provenance. Arbitrary libraries cannot use this partial callback table safely.

Run without arguments for managed environment, pending exception, ABI prefix, callback and taint metadata checks. Two arguments identify the verified Windows x64 fixture path and binary hash. Linux x64 glibc additionally requires the verified bridge path/hash; an optional final probe path/hash enables the isolated ABI probe. Native and original-OTP observations are reported separately.

Windows provides ten fixed callbacks in the pinned 180-slot table. Linux adds actual C variadic tuple/list traversal to those ten operations. Other callbacks, native-created threads, resources, binaries, dirty scheduling, upgrades and general older ABI compatibility remain unfinished.

The registry owns admitted reservations, ready images and queued retirement until actual initialization/call unwind and owner cleanup finish. Deleting a module preserves its old image. Successful purge completes its code-reference protocol and queues cleanup; its public result does not wait for the asynchronous unload callback. The custom high-level pending-discard API joins its cleanup. The raw failed-on_load prepare/purge protocol remains incomplete. Runtime disposal and halt quiescence join owned cleanup; immediate/deadline halt results retain their existing timing.

The bounded native provider requires the default AssemblyLoadContext. One provider-wide synchronous fence excludes initialization and unload across its owners; admitted function calls retain the per-owner monitor. This restriction applies to native loading, while managed language runtimes can still use other assembly contexts. The native tool checks two real owners with controlled barriers, cancellation before native entry, retry after permit release, unload counters and refusal of a duplicate provider context. These controls do not establish arbitrary callback, resource or full VM lifecycle compatibility.

C# owns terms, environments, pending exceptions, allocation tracking, loading policy, bindings and lifetime decisions. Under work.md section 2.1, the three implementation files in `native/` provide only ELF symbol forwarding, pointer-table publication and unavoidable C `va_list` traversal. The fourth C source is an ABI test probe. No language/runtime algorithm is implemented in C.

Build these sources only with the actual configured OTP-29.1.1 headers from commit `ad05823719d77c8faee87348ea39513d4e2f99c5`. Upstream Apache-2.0 headers remain external, hash-verified build inputs; this directory does not redistribute those headers or native binaries. Linux source/build provenance and the fresh original observations belong to the dedicated manual validation workflow.

The native fixture command also compiles the Erlang source witness and checks real generated-source entry routes. Its internal installation bridge is deliberately called outside tail position; implementing the dedicated public load_nif caller-capture instruction remains separate. Runtime does not reference Compiler: the development tool references both and links the existing Roslyn compiler helpers.
