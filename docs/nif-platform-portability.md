# NIF platform portability — implementation and evidence

Checkpoint: 2026-11-05. **Partial implementation; full OTP platform parity is not accepted.**

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

| Host | Code path | Evidence from this checkpoint |
| --- | --- | --- |
| Windows x64 | Existing callback model; C varargs adapter now also exports rejecting `set_option` | Native C smoke passed; focused managed checks passed |
| Windows ARM64 | Same callback model with ARM64 native adapter, native `set_option` required | DLL and native smoke executable cross-compiled; **not executed** |
| Linux x64 glibc | Existing isolated glibc namespace path retained | Managed host-side backend checks only; Linux execution **not rerun** |
| Linux ARM64 glibc | Same namespace path, AArch64 ELF machine and actual host page alignment | Code and generated-header checks only; **not target-compiled or executed** |
| Linux LP64 without the namespace profile, including musl | Ordinary process-global Unix exports | Source path only; **not target-compiled or executed** |
| macOS x64 / ARM64 | Native dyld/libc adapter, process-global `enif_*` exports | Source path only; **not target-compiled or executed** |
| FreeBSD LP64 | Same target-compiled Unix adapter | Source path only; **not target-compiled or executed** |

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

The builder checks both public NIF headers byte-for-byte against the pinned Git
objects, verifies native layout and loader error behavior, and records host,
compiler, sources, headers and hashes. Put the resulting `Erlang.NifBridge.so`
beside the host executable, or in `runtimes/<target-rid>/native` before building
the runtime package. No Unix binaries are supplied or certified by this
Windows checkpoint. `.so` is the OTP NIF suffix on Darwin too; the bridge itself
is built as a Mach-O dynamic library there.

For managed references, MSBuild selects native output files using
`RuntimeIdentifier`, falling back to the SDK host RID. All available native
RID files retain their NuGet `runtimes/<rid>/native` paths. The Windows ARM64
asset must never overwrite the x64 asset in a host output directory.

The manual `NIF platform portability (target execution)` workflow builds pinned
OTP and the bridge on the selected Unix runner, then tests native callbacks,
independent environment ownership, tuple17 varargs and the initializer alias.
It has **not been dispatched** by this checkpoint. Its smoke is not the full
NIF lifecycle/differential suite.

## Explicit remaining gates

- Run actual native builds and integrated NIF/lifecycle/resource/thread/upgrade
  comparisons on macOS Intel/Apple Silicon, Linux ARM64 and BSD/musl.
- Verify Mach-O symbol binding for unmodified OTP-built NIF bundles. The current
  probe explicitly links the bridge. OTP's `-bundle_loader beam.smp` cannot be
  assumed equivalent to a .NET executable; source support does not prove binary
  compatibility for arbitrary existing bundles.
- Reviewed SHA256/descriptor-based target loading and its reallocation extension
  remain specific to the glibc path. The portable loader explicitly rejects
  reviewed admission rather than validating one file and loading another path.
- Native layout and physical resource/GC assumptions need target evidence.
- 32-bit NIF layouts are **not implemented**. Windows x86, ARM32 and other
  pointer-size-4 hosts are rejected, not classified as verified.
- OTP hosts without a usable .NET host, and other Unix OS profiles such as
  Solaris/AIX/OpenBSD/NetBSD, are **not implemented or accepted** by this change.
  Matching OTP's complete platform set requires further host/ABI work.
- No whole-module/MFA acceptance, full regression, performance or full platform
  parity claim follows from this checkpoint. Existing canonical readiness and
  compatibility files are preserved because they contain unrelated dirty work.

See [scoped evidence and English checkpoint](validation/nif-platform-20261105/README.md).
