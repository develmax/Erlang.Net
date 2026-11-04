# DETS backend selection

Baseline: OTP-29.1.1, commit `ad05823719d77c8faee87348ea39513d4e2f99c5`.
Both alternatives expose the same 64 explicit `dets` exports. Compiler-added
`module_info` functions are metadata, outside that count.

## Selecting an implementation

Select once before starting DETS or opening any table. The runtime's startup
selection accepts the following managed module choice:

```csharp
RuntimeModuleSelection.Managed("dets", new[]
{
    "dets_server", "dets_sup", "proc_lib", "ets", "sys", "qlc",
    "file", "io", "io_lib", "logger", "lists", "erlang"
});
```

Supply the listed modules and their dependency closure in the same
`RuntimeLibrarySelection`. The constructor rejects an incomplete selection.
For original Erlang use `RuntimeModuleSelection.Beam("dets", originalDetsBeam,
dependencies)` with the packaged pinned image and its imports. A manifest uses
`Backend: "Managed"` or `Backend: "Beam"`; BEAM entries also require a relative
image path and SHA-256. See `RuntimeLibraryManifest.Load` for the schema.

For an explicitly composed registry, call `DetsModule.Register(registry)` before
kernel boot to install C#, or publish/load the original `dets.beam` for Erlang.
The permanent tests exercise these two compositions through real kernel boot,
original ETS, the file server and actual disk resources. They verify the source
owner as well as results. Whole release-manifest startup is a separate gate.

Both choices use the unchanged Erlang `dets_server` and `dets_sup` on this .NET
runtime for registration, supervision and ownership. The C# table worker and
v9 engine replace `dets`, `dets_v9` and `dets_utils` logic; they do not invoke
those original implementations as a fallback. Supporting `proc_lib`, `sys`,
QLC, ETS, file and logging modules retain their explicit runtime selections.
Production execution does not require an external ERTS process.

The boot composition must initialize the original `prim_file:on_load/0` and
`prim_buffer:on_load/0` native owners, as the VM does. The acceptance helper
initializes raw files before kernel boot and the buffer owner before DETS use.
Raw file and RAM driver capabilities require the bootstrap filesystem profile.
An ordinary minimal registry without these dependencies cannot run DETS.

Do not change backend while tables are open. Continuations contain process
identity, allocation state and compiled match-spec resources; keep them within
the selected runtime. `repair_continuation/2` follows OTP resource lifetime rules.
There is no live backend migration.

## Storage and behavior

The C# implementation uses the native DETS v9 format, linear hashing, buddy
allocation, dirty/save protocol, persisted free lists, header checksums and
trailers. Mutations update the disk engine incrementally. Repair streams
collections through a temporary disk file. RAM-file tables use the runtime's
`ram_file_drv` command protocol. Set, bag, duplicate_bag, counters, callbacks,
match/select continuations, fixation, bchunk, QLC, ETS transfer, owner death,
readonly files and legacy v9 variants have focused OTP comparisons.

Original Erlang sources, BEAM assets and notices remain unchanged. The original
OTP executable is a development oracle only. See the
[acceptance record](validation/dets-port-20261104/README.md) for exact tested
scope, identities and limitations. Finite Windows checks cannot establish a
universal 100% compatibility claim across all schedules, resources and systems.


## Platform deletion boundary

`del_file_nif` now calls `PrimFileDeleteOperations`. That shared C# boundary
selects a Windows or Unix implementation before any path marshalling and builds
`ok` or `{error, Reason}` uniformly. Windows uses UTF16/path handling and
DeleteFileW, with OTP directory/invalid-name error adjustments. Linux/macOS use
libc unlink with the original NUL-terminated byte sequence. There is no filename
UTF8 decode/re-encode, File.Exists precheck or Win32 call in the Unix branch.
Linux and Darwin errno values are mapped independently; EISDIR becomes EPERM,
and unknown numbers retain OTP errno_N formatting.

This is the deletion boundary, not a promotion of the entire runtime. The older
bootstrap filesystem registration still explicitly requires Windows, and other
path/info/raw-file/kernel/native-admission boundaries retain platform gates.
A Unix unlink implementation does not make complete DETS/kernel startup on
Linux/macOS accepted. The standalone portable probe exercises this operation
without that Windows bootstrap profile. See the [current platform receipt](validation/prim-file-platform-delete-20261104/README.md).
