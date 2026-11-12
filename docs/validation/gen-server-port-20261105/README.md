# gen_server — final focused acceptance, 2026-11-05

The complete42-export C# alternative is implemented alongside byte-identical original OTP29.1.1 Erlang/BEAM. Both run on .NET under the same MFA API. Use `RuntimeModuleSelection.Managed("gen_server")` or the explicit original `Beam` selection; see the [backend contract](../../gen-server-backends.md).

## Final results

| Check | Actual result | Evidence |
|---|---|---|
| Clean isolated full project build | 0 warnings,0 errors; no source exclusions | [build](final-107/build-024-isolated.log) |
| C# backend |107/107 full API observations;70/70 scenarios | [report](final-107/Managed-report.json) |
| Original Erlang backend on .NET |107/107 full API observations;70/70 scenarios | [report](final-107/Beam-report.json) |
| Unchanged upstream tests |4/4 on each backend: calling_self, get_state, replace_state, reply_by_alias_with_payload | Reports and retained full gen_server_SUITE image |
| Pinned original source/full suite,42 exports,semantic string ownership | Pass | [audit](final-107/source-audit-004.json) |
| SDK source layout |0 files need formatting | [layout](final-107/style-check-004.log) |
| Corrupted corpus identity | Rejected before any product execution | [control](final-107/negative-controls.json), [rejection](final-107/rejection.log) |

[Acceptance summary](final-107/acceptance.json), [exact source/assembly identity](final-107/execution-identity.json), [tool identity](final-107/tool-identity.json) and [oracle provenance](final-107/provenance.json) bind this checkpoint. ETF inputs, exports, feature state and actual observer/companion/full-upstream BEAM files are retained under final-107/. The source/observer changes are [reviewable as a scoped patch](source.patch), relative to20e02eb941ef3b99cb675827ef0f20663da7c04b. That clean source snapshot contains the port and only the connected call_fun badfun/badarity capture helper/hunk; it excludes the unrelated caller-save/GC/send and other library drafts. The shared checkout had many pre-existing concurrent modifications.

## Commands

From the original repository root, capture into a fresh directory:

```powershell
pwsh -NoProfile -File tools/validate-gen-server-original.ps1 -Output artifacts/gen-server/oracle-012
```

The isolated source tree is the listed base plus source.patch. In that tree:

```powershell
dotnet build tools/Validation/Erlang.GenServerCompatibility/Erlang.GenServerCompatibility.csproj -m:1 --artifacts-path artifacts/build
dotnet artifacts/build/bin/Erlang.GenServerCompatibility/debug/Erlang.GenServerCompatibility.dll . ../isolated-corpus-107 Managed
dotnet artifacts/build/bin/Erlang.GenServerCompatibility/debug/Erlang.GenServerCompatibility.dll . ../isolated-corpus-107 Beam
```

The original shared tree also passed an earlier normal focused build020. The earlier104-case snapshot is retained under final/ as historical evidence. Its103-case observations are historical; the final107-case isolated reports supersede them. Reproduction instructions and the separate admission control are under tools/Validation/Erlang.GenServerCompatibility and tools/Validation/test-gen-server-admission.ps1. The Python audit is tools/Validation/audit-gen-server-source.py; the actual interpreter used here was Python312. SDK layout ran against a copy of only the owned files and the formatted files were copied back; no unrelated source was reformatted.

## Comparison scope

Results compare complete terms and complete captured exception stacks, with no frame/reason removal. Effects cover actual local/via startup, call/cast/info/continue, all action forms, normal/zero/absolute timers, aliases, request collections, broadcast/multi_call, sys state/code-change/debug/status, callback failures/throws, parent exits and exactly-once termination. Known caller/server PIDs are projected separately to named markers only where process identities are inherently different. Every effect runs in a fresh process and linked children are cleaned up. Original gen/proc_lib/sys/io/net_kernel and a real code-server boot execute on .NET. The group leader is the existing runtime I/O device; no gen_server/sys/proc_lib/CT expected value is substituted. Logger primary delivery is disabled; formatting and exception values are checked separately.

The final three additional contracts verify literal Erlang `case catch` semantics for values thrown by a caller-provided via registry, including thrown `{ok,Value}`, thrown EXIT and a case-clause error. Those returns are implemented in C# rather than rewritten as unconditional exits.

## Preserved failures

The [initial reports](history/initial-Managed.json) retain the extra managed source-wrapper and missing callback frames; the matching [Beam report](history/initial-Beam.json) retains the missing PID projection. [Upstream-environment reports](history/upstream-environment-Managed.json) retain the missing I/O group leader/helper and process-isolation failures. The [isolated runtime gate](history/isolated-runtime-gate-Beam.json) retains102/103 with the missing failed-fun call-site frame. No earlier failed run is relabelled final.

## Limits and publication

Finite Windows results do not establish universal100% OTP compatibility. Full gen_server_SUITE, distributed/global integration, every resource/scheduler/owned-heap interleaving, cross-platform behavior, actual structured Logger delivery and metadata parity remain unverified. No full regression, validate.ps1 or canonical-readiness regeneration was run, as requested. No compatibility-ledger promotion or performance claim is made.

This checkpoint is local. No commit, push, PR, merge, hosted CI or package publication is claimed. The scoped source patch and exact isolated identities are ready for publication review; preserve the interleaved dirty checkout and existing changelog volumes when publishing. Detailed English checkpoint history is Part582 in0105 and its dated supplements in the active changelog volume.
