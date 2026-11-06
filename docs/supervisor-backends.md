# supervisor backends

The C# alternative implements all 24 non-`module_info` exports of OTP-29.1.1
`supervisor`, including generated `behaviour_info/1`. The original Ericsson
Erlang source and BEAM remain unchanged. Selection happens at runtime startup;
there is no automatic fallback or migration of running supervision trees.

The managed selector entry, composed with the selected release's helper closure,
is:

```csharp
RuntimeModuleSelection.Managed("supervisor")
```

The original alternative uses
`RuntimeModuleSelection.Beam("supervisor", originalBytes, dependencies)` in the
same startup selection mechanism. Include the original helper closure and its
native capabilities explicitly, as required by startup dependency admission.
For either alternative, use the complete selected `gen_server` implementation
and its `gen`/`proc_lib`/`sys` closure plus the Logger/formatting helpers. The old
default host-facing gen_server facade is not the accepted helper configuration.
The differential runner publishes the original supervisor and original helper
images after startup, and checks every export's actual source owner. Its Beam
report does not claim a full release-manifest startup test.

Both implementations use the same Erlang MFA API. The C# implementation owns
the supervisor algorithms. It calls shared process primitives and the selected
`gen_server`, `sys`, Logger and formatting helpers; it never executes original
`supervisor` to implement a managed operation. The pre-existing host-facing
`Erlang.Otp.Supervisor` API remains available separately.

The implementation includes map/tuple flags and child specifications, all four
restart strategies, restart intensity and retry casts, significant children,
automatic shutdown, dynamic children, monitor-based shutdown with unlink/EXIT
cleanup, parallel dynamic shutdown, hibernation actions, code upgrades and
legacy/current Logger report callbacks. Guest state uses the original `state`
and `child` tuple layouts. Each callback updates a private copy and publishes
new immutable terms, so `sys` inspection and replacement remain observable.

Reproduce focused validation with:

```powershell
pwsh -File tools/validate-supervisor-original.ps1 -Output artifacts/supervisor/new-oracle
dotnet build tools/Validation/Erlang.SupervisorCompatibility/Erlang.SupervisorCompatibility.csproj -m:1 -c SupervisorValidation --artifacts-path artifacts/supervisor/build
dotnet artifacts/supervisor/build/bin/Erlang.SupervisorCompatibility/supervisorvalidation/Erlang.SupervisorCompatibility.dll . artifacts/supervisor/new-oracle Managed
dotnet artifacts/supervisor/build/bin/Erlang.SupervisorCompatibility/supervisorvalidation/Erlang.SupervisorCompatibility.dll . artifacts/supervisor/new-oracle Beam
pwsh -File tools/Validation/audit-supervisor-source.ps1
```

Oracle capture verifies the admitted official release's executable/library
closure before and after execution. Replay checks source, original BEAM,
observer BEAM/corpus, unchanged upstream testcase files and their BEAM hashes.
Reports record actual source owners and runtime/managed assembly identities.
Dynamic PIDs/references are projected only where the observation explicitly
marks them; ordinary outcomes and captured error stacks are compared in full.
Map key sets are sorted explicitly because map traversal order is unspecified.

Finite Windows acceptance does not certify universal 100% OTP equivalence.
Remaining acceptance includes the rest of `supervisor_SUITE`, arbitrary malformed
internal callback states and complete source-stack/Logger metadata boundaries,
distributed/global/via naming, platform/resource extremes and adversarial
scheduling. Shared runtime behavior constrains both alternatives. Full regression,
global readiness and whole-release startup acceptance are separate checks and
were not requested for this work.
