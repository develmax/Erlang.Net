# Part589 — rpc implementation and focused acceptance

The complete selectable C# rpc library implements all **35 explicit exports**.
Original OTP-29.1.1 rpc source/BEAM are retained and independently selectable.
Implementation preceded verification. [Backend/dependency contract](../../rpc-backends.md).

The ordinary focused project build passes with **0 warnings and 0 errors**:

```powershell
dotnet build tools/Validation/Erlang.RpcCompatibility/Erlang.RpcCompatibility.csproj -m:1 -v:q
```

See [build.log](build.log), [source/binary identities](source-and-binaries.json)
and [machine-readable summary](summary.json). Final acceptance uses a private
copy of that ordinary build's output, preventing concurrent drafts from changing
the tested binaries. All 12 backend/profile runs bind the same Kernel, Runtime
and Stdlib assembly hashes. There are no final build exclusions; rpc/erpc/rex library bodies are not stubbed. Connected fixtures use explicit host controllers for packet forwarding.
The shared working tree includes unrelated uncommitted ports; this is not an
isolated main-branch publication or hosted CI claim. The [portable PDB audit](pdb-source-audit.json) verifies22 owned source documents against their compiled checksums. Five other shared source files changed after the snapshot (Logger, GenServer and interpreter/error helpers); those paths are explicit in the audit. Final results describe the frozen assembly set, not subsequent concurrent edits.

## Final matrix

Each cell is exact API observations / protocol scenarios. The scenarios include
the unchanged `rpc_SUITE:off_heap/1` testcase.

| Backtrace depth | C# Managed | Original Erlang/BEAM on .NET |
| --- | --- | --- |
| 0 | [241/241; 18/18](depth0/Managed-report.json) | [241/241; 18/18](depth0/Beam-report.json) |
| 1 | [241/241; 18/18](depth1/Managed-report.json) | [241/241; 18/18](depth1/Beam-report.json) |
| 2 | [241/241; 18/18](depth2/Managed-report.json) | [241/241; 18/18](depth2/Beam-report.json) |
| 3 | [241/241; 18/18](depth3/Managed-report.json) | [241/241; 18/18](depth3/Beam-report.json) |
| 8 | [241/241; 18/18](depth8/Managed-report.json) | [241/241; 18/18](depth8/Beam-report.json) |
| 32 | [241/241; 18/18](depth32/Managed-report.json) | [241/241; 18/18](depth32/Beam-report.json) |

Both backends also pass **13/13 actual distribution-packet checks** between two
.NET runtimes: [Managed](depth8/Managed-connected.json),
[BEAM](depth8/Beam-connected.json). These cover remote call/block_call, EXIT
conversion, async/yield, non-abandoning nb_yield timeout, mixed local/remote/
missing-node and duplicate multicall, cast, eval_everywhere, server_call,
multi_server_call, rex sbcast acknowledgements/missing names and remote pinfo.
Packets pass through the runtime distribution codec and input/output adapters.
External socket/ERTS interoperability is a separate acceptance dimension.

## Reproduction and provenance

The checked-in `tools/validate-rpc-original.ps1` accepts a new output directory
and optional `-BacktraceDepth`, `-RunnerDll` and `-CaptureOnly`. The final runs use:

```powershell
pwsh -NoProfile -File tools/validate-rpc-original.ps1 `
  -Output artifacts/rpc-verified-depth8-20261105 -BacktraceDepth 8 `
  -RunnerDll artifacts/rpc-final-bin-20261105/Erlang.RpcCompatibility.dll
```

Other profile directories were `artifacts/rpc-final-depth0-20261105` through
`rpc-final-depth3-20261105` and `artifacts/rpc-verified-depth32-20261105`, with the
corresponding depth argument. The script runs both implementations and, at
depth8, both connected checks. Captures, observer/suite BEAMs, ETF data, provider
receipts, JSON results and command logs are preserved in each depth directory.

The oracle is the admitted official OTP-29.1.1 release at source commit
`ad05823719d77c8faee87348ea39513d4e2f99c5`. The retained rpc source blob equals
the pinned upstream blob `79deb1aae6cf70169178eceec51ee11d5dec9ac3`. The retained
rpc BEAM equals the admitted release image with SHA-256
`C0177EB89FB41F1E5C93A2CAA24231C79678AC1154D61DCA8EADC15B55141BB8`.
The unchanged reference suite is checked against its pinned git blob. Provider
execution files are checked before and after capture. The extra retained-rpc
identity guards were added after depths0/1; their unchanged inputs were also
verified independently. This validation-only revision is explicit in the source
manifest; compiled C# and observer inputs did not change across final profiles.

The SDK layout tool reports **0 files needing formatting** over a scoped mirror
of the owned sources. Whitespace checks over the shared integration paths pass.
No full regression, `validate.ps1`, performance sweep or canonical-readiness
regeneration ran under this focused task.

## Compared contracts and projections

Compare complete values, reasons, stack frames/error metadata and effects.
Tests cover invalid nodes/MFAs/lists/timeouts/request keys; all public API
families; callback states; node observer restart; async request ownership;
serialization through rex; Unicode/Latin1 stdout; nested I/O requests; unsupported
requests; malformed I/O lists; and the upstream Unicode-error `state` quirk,
including a later badrecord and linked-process/monitor reply.

Only three identity projections are used. Async keys must contain two distinct
references and are consumed through yield. The captured eval_everywhere callback
is compared by its module, name, arity and exact free environment, not its
implementation-specific code hash/index. The observer PID inside C-node failure
state is renamed only when it equals the actual caller. Remaining data and full
stacks are compared. Lifecycle tests await actual DOWN and avoid a scheduler
latency assertion for a successful short call.

## Earlier failures and remaining gates

[Saved earlier reports](history/) retain incomplete initial and expanded
comparisons. Repairs include cons-versus-proper-list guards, implicit timeout
arguments in function_clause frames, pmap/map_nodes continuations, callback
maps/reply errors, native send metadata/source frames and C-node failure stacks.
The original oracle initially used the supervised rex; the final fixture uses
an unsupervised test rex and restores the original registration. Positive
10ms calls were changed to a generous deadline because the original BEAM path
could time out under development load. These are fixture corrections, not a
scheduler-parity claim. [Build history](history/build-failures.md) distinguishes
temporary frozen dependency builds from the final ordinary successful build.

This is finite Windows acceptance, **not universal 100% OTP certification**.
The full peer/old-release/EI upstream suite, external ERTS transport,
resource/exhaustion/scheduler races, physical heap/hibernation, other platforms,
function/module metadata identity and complete release-startup manifests remain
unverified. Global readiness is not promoted from these partial reports.
No commit, push, PR, merge or hosted CI is claimed from the interleaved draft tree.
