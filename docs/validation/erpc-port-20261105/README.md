# erpc selectable implementation — 2026-11-05

The complete C# library implements all **33 explicit OTP-29.1.1 exports**, replacing the earlier three-export facade. The original Erlang source and BEAM are unchanged. Both backend owners and their exact export sets are checked after startup and after ordinary Kernel registration. The latter preserves a selected erpc instead of reinstalling defaults. The legacy rpc:call facade also dispatches through the selected erpc MFA instead of directly invoking the C# implementation.

The [backend contract](../../erpc-backends.md) describes selection and acceptance boundaries. [summary.json](summary.json) binds the observed matrix to source hashes, the baseline and the shared checkout HEAD. Each backend report also records the actually loaded Erlang assembly hashes. These files are local evidence; no hosted CI or publication is claimed.

## Final matrix

| Check | Managed | Original BEAM on .NET |
| --- | ---: | ---: |
| Backtrace depth 0 | [330/330](depth-0/Managed.json) | [330/330](depth-0/Beam.json) |
| Backtrace depth 3 | [330/330](depth-3/Managed.json) | [330/330](depth-3/Beam.json) |
| Backtrace depth 8 | [330/330](depth-8/Managed.json) | [330/330](depth-8/Beam.json) |
| Backtrace depth 32 | [330/330](depth-32/Managed.json) | [330/330](depth-32/Beam.json) |

Each profile has 309 complete API/error-stack observations, 18 stateful groups and three unchanged upstream erpc_SUITE cases: call_always_spawn, multicall_always_spawn, timeout_limit. Both alternatives execute the same authentic observer and original lists helper; maps and erlang are explicit common runtime selections. No original erpc function is called by the managed library.

The corpus checks fun and MFA overloads, all exception classes and internal exports, argument overflow, source-stack trimming with malformed tails, caller versus spawned execution, collection labels/deletion/errors, request cancellation and late-message suppression, input-order multicall results, disconnected targets and mailbox effects. Projection replaces dynamic PID/reference/fun identities only; some scenarios intentionally return identity predicates/counts rather than raw dynamic terms. Complete deterministic outcomes, reasons, metadata and stacks are compared.

[Connected checks](depth-8/connected.json) pass 8/8: local RPC exceptions, queued DOWN versus zero timeout, abandonment losing to admitted DOWN, visible labels and node retirement, remote spawn abandonment, request/reply/death, demonitor after success and stale reply rejection. These existing methods are replayed from the explicitly hashed frozen Erlang.Tests assembly over current normally built runtime/kernel dependencies. This is not a rebuild or full execution of the test harness. The source fixture and differential runner in tools/Validation/Erlang.ErpcCompatibility are permanent current-source tests.

The ordinary focused [build](build.log) passes with **0 warnings and 0 errors**, without source exclusions or BuildProjectReferences=false. [SDK layout](style.log) reports zero files needing formatting for the staged owned source set. No unrelated files were reformatted.

[Negative controls](negative-controls.json) independently reject a changed source identity and corrupted ETF capture with exit code 2. The runner checks the pinned baseline, current source and corpus hashes, exact export sets/owners and the expected total of 330 observations. Its initial count guard incorrectly attempted to decode captured local funs without runtime admission; the final runner counts complete observations after authentic runtime import instead.

## Provenance and reproduction

The original erpc source matches `git -C ../otp show OTP-29.1.1:lib/kernel/src/erpc.erl` byte for byte. The unchanged erpc_SUITE fixture matches the same pinned tag byte for byte. The packaged erpc.beam matches the admitted official release inventory. Provider manifests, executable/compiler/runtime BEAM closure hashes and capture/source hashes are recorded per profile, for example [depth-8 provenance](depth-8/provenance.json).

```powershell
dotnet build tools/Validation/Erlang.ErpcCompatibility/Erlang.ErpcCompatibility.csproj -m:1 --nologo -v:q
pwsh -NoProfile -File tools/validate-erpc-original.ps1 -Output artifacts/erpc-new-depth8 -BacktraceDepth 8 -CaptureOnly
dotnet tools/Validation/Erlang.ErpcCompatibility/bin/Debug/net10.0/Erlang.ErpcCompatibility.dll . artifacts/erpc-new-depth8 Managed
dotnet tools/Validation/Erlang.ErpcCompatibility/bin/Debug/net10.0/Erlang.ErpcCompatibility.dll . artifacts/erpc-new-depth8 Beam
dotnet tools/Validation/Erlang.ErpcCompatibility/bin/Debug/net10.0/Erlang.ErpcCompatibility.dll . artifacts/erpc-new-depth8 Connected
```

Use separate new capture directories for depths 0, 3 and 32. The script rejects an existing output path, a different pinned source/BEAM/suite or modified admitted provider files. Each profile directory preserves its original ETF inputs, compiled observer/suite, logs, source/provider manifest and final reports.

Implementation was completed before validation began. Keep [historical logs](historical/) as actual earlier failures: unrelated concurrent source compilation, source-stack mismatches (287/326 and 329/330), incomplete startup dependencies and an initial locked-output rebuild. Temporary structured reports were subsequently replaced during development; the historical logs are not relabeled as final acceptance. The final matrix above comes from separate admitted directories replayed after the rpc routing fix. Earlier passing binary identities remain under [before-rpc-routing](before-rpc-routing/); they are not substituted for the final reports.

## Remaining acceptance and delivery

API implementation parity and finite Windows observations do not certify universal **100% OTP compatibility**. Remaining acceptance includes the rest of erpc_SUITE with real peers, old releases and EI nodes; complete distribution/timing/resource failure interleavings; cross-platform behavior and metadata/physical scheduling. There is no performance claim or live migration. The full regression, validate.ps1 and canonical readiness regeneration were deliberately unrun under the user's focused-check instruction; component scope is updated without promoting the global ledger.

All work remains local in a shared dirty checkout with independently evolving library/runtime changes. The final normal build succeeds in that checkout, but it is not an independently isolated publication tree. No commit, push, PR, merge or hosted CI is claimed. Publishing must isolate the erpc integration and required shared changes without including unrelated ports.
