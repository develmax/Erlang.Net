# Part583 — supervisor focused acceptance

2026-11-05. Baseline: **OTP-29.1.1**, commit
`ad05823719d77c8faee87348ea39513d4e2f99c5`.

The C# library implements all **24 non-module_info exports** beside the unchanged
original Erlang/BEAM alternative. This checkpoint completes the implementation
and the focused acceptance below; it does **not** certify universal 100% OTP
compatibility. See [selection and remaining acceptance](../../supervisor-backends.md).

| Final check | C# Managed | Original BEAM on .NET |
| --- | ---: | ---: |
| Full API observations, including returned errors/captured stacks | 287/287 | 287/287 |
| Stateful scenarios, including 19 unchanged upstream testcases | 105/105 | 105/105 |
| Failures | 0 | 0 |
| Actual export ownership check | 24/24 managed wrappers | 24/24 original source owners |

Final reports: [Managed](oracle-006/Managed-report.json) and
[Beam](oracle-006/Beam-report.json). [Oracle provenance](oracle-006/provenance.json)
binds the admitted official executable/library closure, observer source/BEAM,
unchanged upstream sources/BEAM and ETF inputs. The reports bind managed/runtime
assembly hashes, helper image identities and managed source hashes.

The corpus covers flag/specification validation, arbitrary-size valid integer
flags, all public API categories, direct exported callbacks, both Logger
formatters, original state/child tuple layouts, sys/code upgrades, names,
startup errors/rollback, extra startup values and temporary argument retention.
Effects include four strategies × three restart policies × four exit reasons,
significant-child policies, restart-intensity exhaustion, failed-start retry
casts, child order, exact integer/float IDs, unlinked children, hibernation,
shutdown modes, dynamic parallel shutdown and a barrier that requires all
children to receive shutdown before any is released.

The 19 unchanged upstream testcase functions include the full
`child_specs_map` product of **7,200 combinations**. The Erlang interpreter path
took longer than the initial deadlines; the final capture and replay permit
300 seconds for that testcase. No testcase assertion was removed. The observer
normalizes dynamic PID/reference identities only in explicitly marked values;
it sorts the key-set observation rather than assuming a map traversal order.

The [source audit](source-audit.json) verifies Git-blob equality for the original
supervisor source and five upstream suite/helper files against the pinned
reference checkout. [Exact source snapshot](source/) and its
[SHA-256 manifest](source-manifest.json) preserve the implemented component and
the validator. The older `Erlang.Otp.Supervisor` host facade remains unchanged.

Commands actually run, from the repository root:

```powershell
pwsh -File tools/validate-supervisor-original.ps1 -Output artifacts/supervisor/oracle-006
dotnet build tools/Validation/Erlang.SupervisorCompatibility/Erlang.SupervisorCompatibility.csproj -m:1 -c SupervisorValidation --artifacts-path artifacts/supervisor/build --no-restore -v:q
dotnet artifacts/supervisor/build/bin/Erlang.SupervisorCompatibility/supervisorvalidation/Erlang.SupervisorCompatibility.dll . artifacts/supervisor/oracle-006 Managed
dotnet artifacts/supervisor/build/bin/Erlang.SupervisorCompatibility/supervisorvalidation/Erlang.SupervisorCompatibility.dll . artifacts/supervisor/oracle-006 Beam
pwsh -File tools/Validation/audit-supervisor-source.ps1
```

The final build passed with zero warnings/errors. It references runtime core,
stdlib and serialization directly and uses separate build outputs. There are
**no source exclusion filters**. The SDK-backed layout tool formatted an
isolated copy of only owned new C# files, which were copied back. Final source
and assembly identities are recorded in the reports.

Retain the initial [oracle-003 reports](oracle-003/): C#259/260 plus97/101 and
BEAM260/260 plus98/101. Fix the inlined behaviour-info exception and status-tuple
arity. Repair the observer's short deadline, orphan cleanup and unordered key
assumption. The next [oracle-004 reports](oracle-004/) record C#260/260 plus101/101
and BEAM260/260 plus99/101; the latter's remaining testcase timeout and consequent
registered-name interference are superseded by the final complete run, not
erased. Some earlier normal builds also failed on unrelated concurrently edited
kernel/stdlib drafts or shared output-file locks. Those failures are not
supervisor acceptance results.

Full regression, `validate.ps1`, canonical readiness regeneration, full
`supervisor_SUITE`, distributed/platform/resource stress, every malformed private
state/source-stack boundary and actual Logger-handler metadata acceptance were
not run. Original-helper publication after startup is distinguished from a
whole-release startup-manifest test. No performance advantage or complete
platform equivalence is inferred from these finite observations.

No commit/push/PR/merge or hosted CI is claimed. The shared checkout contains
extensive unrelated changes to the same integration files; publication must
first isolate and validate this component without bundling those changes.
