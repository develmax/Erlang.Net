# Part600: native-word32 storage and real x86 NIF execution

Tested source: `6197702ec04e22c6110ebfaa9b0c1d0ffb991c9c`, based on `d9b250f431df0387b80e41cccd3ab9efdb2e22bd`. OTP29.1.1 pin: `ad05823719d77c8faee87348ea39513d4e2f99c5`. Final publication adds documentation and evidence; the runtime, validation tool and workflow bytes remain exactly those of the tested source. [Source freeze](source-freeze.json), [pinned OTP layout definitions](otp-layout-sources.json), [machine-readable scope](status.json).

## Implementation

Native cells retain real pointer-sized words and real GCHandle roots. A shared word layout controls double payload splitting, bignum digits/small boundaries, ordinary binary refcounter/prefix, ordinary references, physical PID-reference arity and external identifier payloads. The existing minor/major collector, ownership gates, header/kind validation, explicit copy boundary and reclamation remain active. Allocation byte multiplication is checked.

Two defects exposed by real 32-bit tests are fixed: ordinary reference and PID-reference payload arities collided, and off-heap validation assumed one 64-bit data word for external PID/port. Bootstrap native bindings can now be captured at either native word width; physical resource families still reject 32-bit execution through their existing guards. C remains the existing ABI adapter/probe; this part adds no assembly or C runtime semantics and no dependency.

## Windows x86 acceptance

[Final Windows x86/x64 CI](https://github.com/develmax/Erlang.Net/actions/runs/41962198595) passes on the exact source above. Official Microsoft CLR 10.0.14 x86 is extracted from a SHA512-pinned archive without system installation. Its report confirms `Architecture=X86`, pointer bytes 4 and real native admission; the x64 job is a control.

- C sizeof/offsetof public ABI and managed layout agree; native initializer and rebuilt/shipped varargs adapters pass.
- Native cell kind replacement reclaims GCHandle roots; wrong-kind reads remain rejected. Six finite double bit patterns include signed zero, subnormal values and the largest finite positive double. Five integer cases cross the 32-bit small boundary and include 80/130-bit signed magnitudes.
- Explicit roots survive minor/major collection. Unrooted owned float reads fail after collection; all tested heap blocks, physical words and rooted handles return to zero.
- A 300-byte binary and genuine ordinary local reference survive collection/export. Five external PID/port/reference cases preserve high bits, creation and up to five reference numbers through explicit source export and receiver import, followed by collection of both heaps. Mailbox/alias/lifecycle completeness is not inferred.
- The retained original BEAM calls load_nif on a real MSVC x86 DLL, executes its native load callback and three functions: the sum of 19 and 23, a 17-element tuple through native varargs, and generation 1 from NIF private state. The resulting ETF bytes match the pinned original-OTP oracle exactly.

Reports: [x86 storage/GC](windows-x86/word-storage.json), [x86 real image](windows-x86/image-checks.json), [x86 ABI](windows-x86/abi-checks.json), [x64 storage control](windows-x64/word-storage.json), [artifact hashes and ETF parity](windows-etf-parity.json). The individual initializer/adapter reports intentionally say full runtime execution=false; actual module execution is established by the separate image report.

The committed BEAM/ETF oracle was produced by pinned original OTP on Linux ARM64, producer run 41939988779. Original Windows/x86 ERTS was not executed here. Source modeling and portable ETF parity do not substitute for that missing target oracle.

[Final CI run/job/artifact identities, including attempts](final-ci.json). All 11 final jobs pass across four workflows; the failed upload attempt remains recorded.

## Regression matrix

| Target | Evidence and scope |
| --- | --- |
| Windows x86/x64 | Final CI passes native-word GC, ABI/adapters and real native image. |
| Windows ARM64 | Real DLL and adapter regression passes; portable committed OTP ETF oracle. |
| Linux glibc x64/ARM64 | Native callbacks, same-host original OTP comparison and unchanged native image replay pass. |
| Linux musl x64/ARM64 | Native/managed callbacks, same-host original OTP comparison and unchanged native image replay pass. |
| macOS ARM64 | Native/managed callbacks and both native image modes pass; unchanged image uses the native CLR process host. |
| macOS x64 | Native tests and unchanged image replay pass. Attempt 1 upload failed with GitHub ENOTFOUND; a selective same-source retry passes including artifact delivery. |
| FreeBSD 15 amd64 | Pinned community CLR 10.0.13, native/managed callbacks, same-host OTP comparison and unchanged ELF replay pass. |

[FreeBSD reports](freebsd-x64/artifacts/nif-platform-ci/managed-checks.json), [FreeBSD ETF parity](freebsd-x64/etf-parity.json), [other downloaded regression artifact hashes/parity](regression-artifact-parity.json), [macOS x64 attempt 1 infrastructure failure](macos-x64-attempt1.json). Saved per-host JSON reports remain under `regressions/`. GitHub artifacts are SHA256-verified against API digests before retaining reports. FreeBSD community CLR acceptance does not imply official Microsoft FreeBSD support or any other FreeBSD CPU/version.

## Commands and exclusions

Local commands, run in the isolated source snapshot:

```text
dotnet build tools/Native/Erlang.NifPlatformValidation -m:1 -p:UseAppHost=false -p:ErlangNifRuntimeIdentifier=win-x86
<isolated-official-x86-dotnet> Erlang.NifNativeBackend.dll --word-storage
<isolated-official-x86-dotnet> Erlang.NifNativeBackend.dll --image <pinned-BEAM> <real-x86-DLL> <managed.etf>
dotnet Erlang.NifNativeBackend.dll --word-storage
dotnet run --project tools/Build/Erlang.Style -- --check <owned-CSharp-staging-root>
```

Build: zero warnings/errors. Source layout: zero files needing formatting. [Local x86 GC](local-x86-storage.json), [local x64 GC](local-x64-storage.json), [local x86 image](local-x86-image.json). CI executes `run-windows-abi-ci.ps1` in each actual process architecture and compares ETF bytes; the other linked workflows build/test on their target hosts.

Unsupported/unverified: native resource/magic headers, atomics/counters allocation and finish_loading resource custody on 32-bit; full PID-reference alias/wire parity; ARM32 and other 32-bit NIF hosts; big endian; Solaris/AIX and managed OpenBSD/NetBSD. OpenBSD/NetBSD retain historical C-only evidence. Full NIF resource/thread/dirty-scheduler/upgrade/teardown/API parity, original 32-bit ERTS, performance and full repository regression are unrun. Registered MFA delta is zero. Global module readiness was neither regenerated from a focused report nor promoted.

Publication uses an alternate Git index and isolated artifact snapshot. This part does not modify the shared dirty checkout/index/branch or the sibling reference OTP checkout. No merge, branch deletion, NuGet publication or unrelated change is included. Historical Part598/599 receipts and closed changelog volumes remain immutable.
