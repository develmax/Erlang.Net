# NIF target CI — 2026-11-05

**Four successful target runs, fourteen jobs. Full OTP platform/API parity remains open.**
The reference is OTP29.1.1 at ad05823719d77c8faee87348ea39513d4e2f99c5.

## Actual execution

| Target / scope | Actual outcome | CI |
| --- | --- | --- |
| Linux glibc x64/ARM64; macOS 15 x64/ARM64 | Five managed/native checks per host, real C NIF sum/tuple17/private state, same-host original OTP ETF comparison | [Main matrix](https://github.com/develmax/Erlang.Net/actions/runs/41944852748), db859002 |
| Windows ARM64 | Six checks against each shipped/rebuilt adapter, target C varargs smoke, actual native DLL image and measured header ABI; portable authenticated BEAM/ETF oracle | Same main matrix; original Windows ERTS **not run** |
| Linux Alpine musl x64/ARM64 | Five checks, real NIF/original OTP comparison and unchanged original binary replay | [musl/BSD](https://github.com/develmax/Erlang.Net/actions/runs/41944852680), db859002 |
| FreeBSD 15 amd64 | Native C public layout/loader smoke | Same musl/BSD run; **no .NET execution** |
| OpenBSD 7.8 / NetBSD 10.1 amd64 | Native C layout, dlopen/dlsym/dlclose, getcwd and missing-file behavior | [BSD preflight](https://github.com/develmax/Erlang.Net/actions/runs/41945480449), a97748a9; **no managed admission or .NET execution** |
| Linux glibc x64/ARM64; macOS x64/ARM64, unchanged original binary | Run original OTP-built images without rebuilding/relinking them; byte-identical ETF. macOS uses the native CLR host and reports ProcessHost=true | [Artifact replay](https://github.com/develmax/Erlang.Net/actions/runs/41946249400), 183dcfe9 |

The replay uses immutable artifacts from successful producer41939988779/c39b1d55,
not a newly invented oracle. It checks pinned headers, bridge source/library,
fixture source, BEAM and original ETF hashes against current sources. macOS
rebuilds only the launcher, not the original NIF. The native host and managed
runtime use the current source. [Source bindings](source-bindings.json) prove
identical complete production src/native-backend trees and validation C#/fixture
blobs at db859002, a97748a9 and183dcfe9. The final publication changes documents
and evidence only. Each single-host report conservatively retains
CrossPlatformExecutionProven=false; the aggregate lists only observed profiles.

The main matrix at db859002 runs the explicitly linked fixture. Its workflow is
extended at183dcfe9 to call the same unchanged-image script that passed replay;
that new main workflow integration is not represented as a separately executed
full matrix. All target production code is byte-identical across those heads.

## Retained fixes and failures

- Disable wx-dependent debugger/observer/et in the minimal original OTP build;
  include the full facade/Stdlib/Kernel host and use the real Alpine SDK tag.
- Expose Darwin RTLD_DEFAULT using target headers; encode observed terms while
  the actor/environment is alive.
- The original Mach-O bundle fails under ordinary dotnet because `_enif_alloc`
  is bound to the main executable. [Retained failure](macos-dotnet-unmodified-failure.json).
  A dylib promotion does not fix executable-ordinal binding. The native host
  exports the existing C bridge from its main image and starts CLR through the
  installed SDK hostfxr API. [First target proof](macos-native-host-first-pass.json),
  superseded by the source-bound x64/ARM64 replay reports below.

## Saved checks and reproducible commands

[Run/head/job/artifact records](ci-runs.json), [verified ZIP digests](artifact-digests.json)
and [machine scope](status.json) accompany copied raw target reports, build
receipts, native host provenance and ETF observations. Every downloaded archive
matches GitHub's SHA256. Every saved ETF equals the pinned original observation.
Artifacts expire independently; the small reports and observations are saved here.

- Unix producer: `sh tools/Native/Erlang.NifPlatformValidation/run-unix-ci.sh`
  on the actual target, with SDK10.0.400 and native compiler/OTP build tools.
- Windows: `pwsh -File tools/Native/Erlang.NifPlatformValidation/run-windows-ci.ps1 -Architecture arm64`
  on actual Windows ARM64; use x64 only on an x64 host.
- BSD C-only: `sh tools/Native/Erlang.NifPlatformValidation/run-bsd-native-ci.sh`.
- Artifact replay: dispatch nif-unmodified-binary.yml with the successful producer
  run ID. No permanent expired producer is hardcoded in the workflow.
- Local final focused build: `dotnet build tools/Native/Erlang.NifPlatformValidation -m:1 --no-restore -v:q`
  in the isolated publication snapshot: zero warnings/errors. Earlier local
  Windows x64 adapter/image execution passed within this continuation; it is not
  labelled hosted final-head Windows x64 evidence. Owned C# layout check passed.

## Remaining gates and checkout ownership

FreeBSD/OpenBSD/NetBSD managed execution, all32-bit layouts, Solaris/SunOS/AIX,
other OS/CPU/version profiles and full NIF API/resource/GC/thread/upgrade parity
remain unverified or unimplemented. Portable Unix has one process-owned VM and
rejects reviewed descriptor/reallocation loading. macOS signing/notarization,
universal binaries and production package deployment remain unrun. No original
Windows OTP, full repository regression, global readiness regeneration or whole
module acceptance is claimed. Registered MFA delta is zero; existing global
acceptance classifications are retained.

All continuation edits stayed in an artifacts publication snapshot based on
main a58bf98a. Publication used an alternate index; shared checkout branch/index
and concurrent dirty files were not switched, staged or reverted. Concurrent
actors changed other checkout files; no claim that all checkout hashes stayed
unchanged is made. No merge, NuGet publication or change to the sibling OTP
reference occurred. See [platform contract](../../nif-platform-portability.md)
and [Part597](../part-597/README.md).
