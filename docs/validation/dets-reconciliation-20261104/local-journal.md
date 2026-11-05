## Part591 — 2026-11-04 — Publish the complete selectable DETS port

**Part status:** implementation and focused local acceptance complete; PR publication.
**Objective and scope:** create an English PR to main for the requested DETS port, preserving the original Erlang alternative and excluding other dirty library work.

### Changes

Add all64 explicit dets exports and explicit managed startup selection. Implement the table worker and native v9(a–d) incremental file engine: linear hashing/growth, buddy/free-list allocation, header checksums/trailers, dirty/save protocol, repair/compaction, RAM/readonly, delayed writes/autosave, set/bag/duplicate_bag, counters, callbacks, fixation, match/select continuations, bchunk, QLC and ETS. Retain original dets_server/dets_sup on .NET for shared registry/supervision. C# never falls back to original dets/dets_v9/dets_utils.

Supply connected declared prim_file storage bindings, all9 prim_buffer primitives, actual ram_file_drv port protocol and raw-file owner-death cleanup. Clear dead BEAM caller-save/argument roots and complete weak managed interop collection after real guest GC. Retained magic refs remain valid. Preserve main's newer bad-function and descriptor-polling implementations. Use separate semantic protocol/layout/source/diagnostic catalogs. Original Erlang sources/BEAM and sibling OTP remain unchanged.

Add authentic-BEAM observers, seven unchanged upstream testcase bodies, four-mode magic lifetime checks and six production-engine/native file interchange cases. Fix observed readonly errno/error poisoning, malformed options, callback source frames, damaged scans, legacy9a behavior and expired continuation resources; retain all earlier failures. [Selection](../dets-backends.md) and [scope](../validation/dets-publication-20261104/README.md).

### Decisions and compatibility

Pin OTP29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5; inspect all five DETS and connected native sources. Reuse canonical portable hashing, match specs, module-owner admission, runtime processes/ports and BCL streams. No new package or private persistence format; retain Apache-2.0 attribution. The user explicitly authorizes this module's managed alternative. Production needs no external ERTS. No live backend migration, performance claim or universal100% certification. Complete upstream/platform/resource/interleaving and release-manifest acceptance remain unverified.

### Validation

Create an isolated checkout from main4f7a8c0ea; do not alter the original mixed checkout or import unrelated ports. The isolated ordinary tests/tool builds have0 warnings/errors. Five DETS groups pass, including21 primary scenarios and7 selected upstream bodies per backend; nine connected GC/ETS/interpreter/raw-file groups pass. Six native interchange cases pass, including700-object growth. Four SDK-only layout repairs have0 remaining in the full final layout check. The formatted-source build has0 warnings/errors and all9 connected checks pass again; tested/final identities are recorded separately. [Immutable reports and exact commands](../validation/dets-publication-20261104/README.md).

The older locally allocated Part581 DETS receipt is preserved in its dedicated historical namespace because main has advanced its own sequential journal. No closed volume is modified. Full regression, validate.ps1, global readiness refresh and manual CI dispatch are unrun under focused scope. Hosted checks and review remain independent publication gates.

### Handoff

PROGRESS, NEXT_STEPS, COMPATIBILITY, semantic differences, dependency decisions and component scope are updated additively on main. Byte-preservation rules bind fixture/provenance identities. Commit only this isolated DETS scope and create the authorized English PR to main; do not merge it. Proposed title: `Add selectable C# DETS backend with native OTP v9 storage`.


## Part592 — 2026-11-04 — Separate Windows and POSIX file deletion like OTP

**Part status:** deletion implementation complete; focused Windows acceptance and Unix source/ABI controls, with Unix host execution unverified.
**Objective and scope:** follow the user's requested OTP architecture: common C# semantics with OS-specific primitives, preserving PR119 and both DETS backends. No full regression.

### Changes

Remove the misleading generic PrimFileDataNative/Names Windows import. Add PrimFileDeleteOperations as the common result/cancellation boundary and isolated Windows/Unix implementations. Dispatch before Windows path marshalling. Windows captures DeleteFileW errors immediately, maps invalid names to EACCES and directories to EPERM as OTP does. Unix passes original terminated byte paths to libc unlink, accepts only trailing NULs and preserves arbitrary bytes/symlink semantics. Linux/Darwin numeric errno tables are separate and filtered through pinned OTP symbolic names; normalize EISDIR and retain errno_N for unknown codes. No File.Exists race or UTF8 roundtrip is introduced in production.

Add focused path/ABI and actual filesystem tests, plus a portable standalone runner linking those same tests without the Windows kernel/bootstrap profile. Extend the existing admitted native observer tool with an exclusive Delete mode and prim_file source/BEAM identity checks. Preserve the original capture and outcomes. The accepted native Windows corpus contains six sequential outcomes and32 concurrent pairs. Original OTP produces27 pairs with two successes,4 with EACCES and1 with ENOENT: compare admitted pair sets and final absence, not frequency/order. Unix retains exactly one success and one ENOENT. The initial single-winner-on-Windows test assumption was disproved by the actual pinned oracle, not hidden by retry.

### Decisions and compatibility

Inspect OTP29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5 Unix/Win32 deletion, raw-path and errno conversion sources; retain Apache-2.0 notices. Inspect public Linux UAPI and Darwin ABI constants without redistributing header bodies. Reuse existing system-library identities and .NET interop, with no new package/helper binary. Windows still uses its OS API inside the Windows adapter; Unix never invokes kernel32. Original Erlang/BEAM remains unchanged.

This change targets deletion only. Existing bootstrap registration and other path/info/raw-file/kernel/native-admission boundaries still prevent claiming a fully portable DETS runtime. Linux/macOS syscall execution is not established by a Windows run; no new bootstrap admission is silently enabled. See [backend scope](../dets-backends.md) and [platform receipt](../validation/prim-file-platform-delete-20261104/README.md).

### Validation

The ordinary focused build has0 warnings/errors. The current deletion/path/ABI/Windows-file groups pass3/3; both DETS primary corpora and seven selected unchanged upstream cases per backend pass4/4. The portable runner is built and run separately; exact results and source/runtime identities are recorded in the receipt. SDK layout passes with0 remaining in the affected-file root. Native capture input identities are checked before and after execution. Keep the initial observer setup rejection for a supplementary Windows filename, the incorrect race test, compiler diagnostic and file-lock retry as historical failures. Accepted oracle cases/DETS expectations are unchanged.

Full regression, validate.ps1, canonical readiness regeneration, shared CI changes/manual dispatch and actual Linux/macOS runs are unrun. No universal100% certification, merge or review-thread resolution.

### Handoff

Update current status, compatibility, dependency decisions and component scope. Commit/push this focused follow-up to PR119 with an English description; leave unrelated source checkout changes intact. Next platform gate: run the standalone probe on Linux/macOS and separately close the remaining bootstrap/native file boundaries. Commit title: `refactor(prim_file): separate Windows and POSIX deletion`.
