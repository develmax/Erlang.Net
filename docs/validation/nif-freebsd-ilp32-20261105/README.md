# FreeBSD .NET NIF execution and Windows x86 ABI checkpoint

Checkpoint date: 2026-11-05. Seven source-bound runs / sixteen jobs passed; the latest source has three targeted jobs in two runs. **FreeBSD finite execution verified; Windows x86
ABI foundation verified, full 32-bit VM/NIF execution remains unavailable.**
Pinned original: OTP29.1.1 / ad05823719d77c8faee87348ea39513d4e2f99c5.
Registered MFA delta: zero. Full OTP/NIF acceptance: false.

## Source-bound target evidence

| Source / run | Scope | Actual result |
| --- | --- | --- |
| be791927 / [FreeBSD](https://github.com/develmax/Erlang.Net/actions/runs/41955974610) | Latest managed source; FreeBSD 15 amd64, community CLR 10.0.13, managed build SDK 10.0.400 | Passed: six native/managed checks, real NIF/original OTP ETF comparison, unchanged original ELF replay |
| be791927 / [Windows ABI](https://github.com/develmax/Erlang.Net/actions/runs/41955974784) | Latest guard/source correction; x86 ABI-only and x64 control | x86 C/C# layouts, shipped/rebuilt varargs, nif_init descriptor and explicit VM gate; x64 also executes the real NIF image |
| 9e2911dd / [Main matrix](https://github.com/develmax/Erlang.Net/actions/runs/41953929189) | Linux glibc x64/ARM64, macOS Intel/ARM64, Windows ARM64 | All five jobs passed; Unix compares actual native images against same-host original OTP and executes the unchanged original image |
| 9e2911dd / [musl/BSD](https://github.com/develmax/Erlang.Net/actions/runs/41953929092) | Alpine musl x64/ARM64; FreeBSD C-only control | All three jobs passed; actual managed/original/unchanged NIF execution on both musl profiles |
| 9e2911dd / [Other BSD](https://github.com/develmax/Erlang.Net/actions/runs/41953928740) | OpenBSD 7.8 and NetBSD 10.1 amd64 | Both C-only checks passed; no .NET execution |
| 9e2911dd / [Earlier FreeBSD](https://github.com/develmax/Erlang.Net/actions/runs/41953928877) and [Windows](https://github.com/develmax/Erlang.Net/actions/runs/41953929427) | Before the final independent prefix/legacy-size guard correction | All three jobs passed; superseded for the latest-source target checks above |

The broad matrix executes at 9e2911dd. The final correction at be791927 changes
only NifNativeVersion651 and the independent Windows ABI probe/checker: restore
explicit expected prefix/legacy sizes and compare them to C offsets. Native
backend source is unchanged. Fresh FreeBSD and x86/x64 target runs check that
correction. Exact heads, tree bindings and the three-file delta are saved; broad
previous-head results are not represented as a full matrix run at be791927.

## FreeBSD failures and fixes

1. [Initial CLR failure](freebsd-initial-openssl-failure.log): the FreeBSD14
   CLR distribution did not probe the FreeBSD 15 libssl.so.35 SONAME. Resolve the
   actual target library, verify OpenSSL 3 and select it through the supported
   DOTNET_OPENSSL_VERSION_OVERRIDE. Observed OpenSSL 3.5.6; crypto/hash verification
   remains active. The runtime archive is pinned by SHA256 in freebsd-runtime.json.
2. [Managed callback failure](freebsd-attempt002/nif-platform-ci/managed-checks.json):
   FreeBSD dlsym(RTLD_DEFAULT) needs a known calling ELF object. The optimized
   wrapper tail-jumped into dlsym from CLR JIT code. [Before machine code](freebsd-dlsym-before.json)
   ends in a jump; [after](freebsd-dlsym-after.json) returns inside the bridge.
   Preserve the call frame with a volatile result. No provider/layout/one-VM
   guard is removed. A managed-origin default-symbol-scope check covers it.
3. Synchronize reports from the VM after failure and check its saved exit code
   outside the VM action; the first failed action skipped copyback.

Initial successful source 6cd54297/run41949862037 establishes FreeBSD execution
before starting x86 ABI implementation. Its reports are retained separately;
latest-source reports supersede acceptance. CLR 10.0.13 is a pinned community
FreeBSD build, not a claim of Microsoft FreeBSD support. Managed compilation
uses the project's SDK 10.0.400; native libraries are compiled on FreeBSD.
Ordinary whole-project SDK 10.0.400 self-build on FreeBSD is not verified.

## What x86 proves

Native C executables compiled and executed as x86 measure pinned public headers;
actual CLR x86 processes compare those measurements with managed structs. This
is target execution on a 64-bit Windows OS, not merely cross-compilation.

| Public type / property | x86 bytes |
| --- | ---: |
| Pointer / C long / event | 4 |
| ErlNifEntry / prefix | 52 / 36 |
| ErlNifFunc / legacy function | 16 / 12 |
| ErlNifBinary | 20 |
| ErlNifResourceTypeInit | 20 |
| ErlNifMapIterator | 28 |
| ErlNifPid / ErlNifPort | 4 |
| ErlNifMonitor | 16 |
| ErlNifTime | 8 |
| TWinDynNifCallbacks | 728 |

A separate win-x86 native asset has a 32-byte varargs registration table. Shipped
and rebuilt adapters pass C smoke and managed callbacks with high-bit tuple17
and list9 words, a fixed 64-bit formatter argument, limits, set_option and
replacement rejection. Raw nif_init from the actual DLL executes, copies the
callback table and returns a descriptor whose fields/functions match C/managed
layouts. **The module load callback and guest NIF functions do not execute in x86.**

An actual [x86 image attempt](x86-local-image-before-runtime-port.json) stops in
ProcessRuntime construction: NativeTermCellBuffer526 requires the admitted
64-bit source profile. CI separately observes that exact known gate. Its green
job means ABI checks and expected gate detection passed; it does not mean a
32-bit guest/NIF module passed. NativeAdmission remains false. Native-cell and
physical-resource guards are unchanged; no fallback representation is added.
Other scalar, lifetime, resource, old-version and GC semantics on 32-bit hosts
remain unverified. ARM32/other ILP32 profiles are not implemented or accepted.

## Receipts, scope and next executable work

Saved [broad CI metadata](ci-broad.json), [latest targeted CI metadata](ci-guards.json), [source bindings](source-bindings.json), [exact guard delta](guard-delta.patch), [archive digests](artifact-digests.json) and [scope](status.json) bind each result to its actual head. Downloaded artifact ZIPs
are checked against GitHub SHA256; small reports/build receipts, runtime/OpenSSL
identity and ETF observations are copied here rather than relying on expiring
artifact links. Individual reports retain CrossPlatformExecutionProven=false;
aggregate observations cover only the listed profiles. Full repository tests,
module-readiness regeneration, performance, signing/notarization, production
package deployment and original Windows ERTS execution remain unrun. Existing
module/MFA acceptance classifications are retained.

Reproduce with nif-freebsd-managed.yml, nif-windows-abi.yml and the platform
workflows. Runtime pins and scripts live in tools/Native/Erlang.NifPlatformValidation.
On x86, keep ABI-only checks separate from --runtime-gate; run real guest image
acceptance only after porting native-cell/word layouts and numeric/binary/reference/
GC dependencies. Retire the expected-gate test only with that new proof.

Continuation edits use the isolated artifacts publication snapshot and alternate
Git index. The shared checkout branch/index and unrelated dirty edits are not
switched, staged, reverted or included. No source changes are made in sibling OTP.
No PR merge or NuGet publication occurs. [Part598](../part-598/README.md),
[Part599](../part-599/README.md) and [platform contract](../../nif-platform-portability.md)
record the finite boundary and remaining work.
