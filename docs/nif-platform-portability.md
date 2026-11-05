# NIF platform portability — implementation and evidence

Checkpoint: 2026-11-05. Four final target runs / fourteen jobs passed; see exact heads in the linked evidence. **Partial implementation; full OTP platform parity is not accepted.**

The reference remains OTP-29.1.1, commit
`ad05823719d77c8faee87348ea39513d4e2f99c5`. No external ERTS is required
in production. A native bridge compiled for the target host is required.

## Existing facilities and reuse decision

The repository already contains macOS/FreeBSD terminal handling, Darwin/FreeBSD
system identity and a macOS libSystem math binding. These are host facilities,
not evidence of NIF portability. Preserve them and reuse BCL `NativeLibrary`,
existing NIF callbacks, allocation domains, environment ownership and lifecycle
admission. No new managed dependency or actor framework is introduced. .NET/BCL
is the existing host integration; another FFI package would not supply our
Erlang term/environment ownership contract. C remains necessary for native
varargs and target-header constants. No third-party implementation was copied.
Preserve existing OTP/Ericsson Apache-2.0 notices.

Inspected pinned original sources:

- `erts/emulator/beam/erl_nif.h`: Windows `TWinDynNifCallbacks`, Unix
  `nif_init(void)`, public entry/function/binary layouts and `ErlNifEvent`.
- `erts/emulator/beam/erl_nif.c`: `va_start`/`va_arg`, integer width conditions.
- `erts/emulator/sys/unix/erl_unix_sys_ddll.c`: `.so`,
  `RTLD_NOW | RTLD_LOCAL`, `nif_init` then `_nif_init`, `dlclose`.
- `erts/emulator/sys/win32/erl_win32_sys_ddll.c`: callback table initialization
  and `LoadLibraryExW`.
- `make/autoconf/otp.m4`: ARM64 normalization and Darwin bundle linkage.

## Implemented paths

| Host | Code path | Actual target evidence |
| --- | --- | --- |
| Windows x64 | OTP callback model and native varargs adapter | Local native/managed smoke and real DLL image passed |
| Windows ARM64 | Target-built adapter, including native rejecting `set_option` | Hosted target execution passed for both shipped/rebuilt DLLs and a real NIF DLL; original Windows ERTS not run |
| Linux glibc x64 / ARM64 | Per-runtime `dlmopen` namespaces, target ELF machine/page alignment | Hosted native/managed checks and same-host original OTP image comparison passed |
| Linux musl x64 / ARM64 | Process-owned native bridge, target libc wrappers | Hosted native/managed checks and same-host original OTP image comparison passed |
| macOS x64 / ARM64 | Target dyld/libc adapter; exported main-executable bridge for OTP-linked bundles | Hosted native/managed checks and same-host OTP comparison passed; unchanged-bundle launcher checks passed on both architectures |
| FreeBSD 15 amd64 | Target-compiled LP64 C bridge | Hosted C layout/loader smoke passed; **.NET runtime and NIF execution unverified** |
| Other LP64 Linux/FreeBSD architectures | Generic portable C ABI path | **Unverified**, including target compilation/execution |
| Windows x86, ARM32 and all 32-bit layouts | Explicit rejection | **Not implemented** |
| OpenBSD 7.8 / NetBSD 10.1 amd64 | C preflight only; managed OS admission disabled | Native C layout/loader smoke passed; **.NET/NIF execution unverified** |
| Solaris/SunOS, AIX and other OTP hosts | No admitted .NET host/ABI profile | **Not implemented or verified** |

The Unix bridge no longer requires `__linux__ && __x86_64__`. It requires LP64
and verifies the actual target C sizes/offsets against managed structures before
publishing callbacks. `va_list` stays opaque to C#: C reads every argument.
`dlopen`, `dlsym`, `dlclose`, `RTLD_*`, `O_CLOEXEC`, `RTLD_DEFAULT` and errno
handling live in the target-compiled C adapter. Linux loader constants are not
reused as Darwin/BSD constants.

The generic Unix path deliberately follows OTP's **one VM per process** model.
It permanently roots its bridge/table/environment allocator and rejects another
`ProcessRuntime`, even after the owning runtime is disposed. Linux glibc retains
its existing per-runtime namespaces. Portable multi-runtime isolation is not
implemented; callbacks are never silently reassigned. Native publication failures
remain failures on repeated admission.

## Build and install

Windows: use `tools/Native/Erlang.NifNativeBackend/build-windows-varargs526.py`
with `--architecture x64` or `--architecture arm64`; the default remains x64.
Pass `--run-smoke` only on the matching actual host, or `--compile-smoke` for
cross-compilation without execution. Supply the actual source SHA256 using
`--source-sha256`. The script emits compiler/linker/export/PE-header receipts.

Unix: build/configure the pinned OTP on the target to obtain its installed
headers, then run:

```sh
python3 tools/Native/Erlang.NifNativeBackend/build-unix-bridge.py \
  --repository . --otp-source /path/to/otp \
  --otp-include /path/to/installed/lib/erlang/usr/include \
  --output artifacts/nif-platform-local --smoke-fixtures
```

The builder checks three public NIF headers byte-for-byte against the pinned Git
objects, verifies native layout and loader error behavior, and records host,
compiler, sources, headers and hashes. Put the resulting `Erlang.NifBridge.so`
beside the host executable, or in `runtimes/<target-rid>/native` before building
the runtime package. Unix binaries are built and retained in target CI artifacts; no cross-host
certification or production package publication is claimed. `.so` is the OTP NIF suffix on Darwin too; the bridge itself
is built as a Mach-O dynamic library there.

For managed references, MSBuild selects native output files using
`RuntimeIdentifier`, falling back to the SDK host RID. All available native
RID files retain their NuGet `runtimes/<rid>/native` paths. The Windows ARM64
asset must never overwrite the x64 asset in a host output directory.

The `NIF platform portability (target execution)` and musl/BSD workflows run
on the dedicated CI branch and via manual dispatch. They configure/build the
pinned original OTP, execute a real C NIF on both VMs and compare ETF bytes.
Windows executes its target C ABI probe and checks a portable authenticated BEAM
and original ETF; that is not an original Windows OTP execution claim.

### macOS executable-bound bundles

A bundle linked by OTP with `-bundle_loader beam.smp` imports `enif_*` from the
main Mach-O executable. Merely promoting a dylib to RTLD_GLOBAL does not satisfy
that binding under ordinary `dotnet`: the unchanged original bundle failed with
`Symbol not found: _enif_alloc`, expected in `dotnet`. This failure is retained.

`build-macos-host.py` builds `erlang-nif-host` from the existing C export,
varargs, platform and layout adapters plus a small hostfxr launcher. Its exports
reside in the main executable, as in OTP. It starts the installed CLR through the
official SDK `hostfxr.h` interface. It contains no ERTS. The managed runtime
recognizes its versioned native marker, verifies the public C layout and uses
that main-executable callback table instead of publishing a second dylib table.
Foreign API providers, a replacement bridge and another VM remain rejected.
The launcher and callback table are process-owned; do not unload their code.

After configuring pinned OTP headers on the actual macOS host:

```sh
python3 tools/Native/Erlang.NifNativeBackend/build-macos-host.py \
  --repository . --otp-source /path/to/pinned/otp \
  --otp-include /path/to/configured/headers --dotnet-root "$DOTNET_ROOT" \
  --output artifacts/nif-process-host
# Supply absolute native-host, hostfxr, SDK-root and application DLL paths:
/path/to/erlang-nif-host /path/to/libhostfxr.dylib "$DOTNET_ROOT" /path/to/app.dll [arguments...]
```

Ordinary `dotnet` remains usable for NIFs explicitly linked to the bridge. Use
the launcher for OTP executable-bound bundles. Arbitrary third-party NIF APIs,
Mach-O deployment/signing/notarization, universal binaries and production package
integration still need their own verification. No global flat-namespace
workaround or original ERTS is required.

## Explicit remaining gates

- Extend the finite image checks to lifecycle/resource/thread/upgrade suites
  on the tested profiles; run .NET/NIF checks on FreeBSD.
- Reviewed SHA256/descriptor-based target loading and its reallocation extension
  remain specific to the glibc path. The portable loader explicitly rejects
  reviewed admission rather than validating one file and loading another path.
- Native layout and physical resource/GC assumptions need target evidence.
- 32-bit NIF layouts are **not implemented**. Windows x86, ARM32 and other
  pointer-size-4 hosts are rejected, not classified as verified.
- OTP hosts without a usable .NET host, and other Unix OS profiles such as
  Solaris/AIX/OpenBSD/NetBSD, have **no implemented or accepted managed NIF
  host admission** in this change. OpenBSD/NetBSD have native C preflight only.
  Matching OTP's complete platform set requires further host/ABI work.
- No whole-module/MFA acceptance, full regression, performance or full platform
  parity claim follows from this checkpoint. Global module readiness is unchanged: no registered MFA or whole-module
  semantic acceptance is added. Current-state documents are updated only in
  the isolated publication snapshot, preserving concurrent checkout edits.

See [hosted evidence and remaining gates](validation/nif-platform-ci-20261105/README.md).
The [earlier local-only receipt](validation/nif-platform-20261105/README.md) is historical.
