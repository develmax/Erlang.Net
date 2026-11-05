# Part591 — Isolated DETS publication acceptance

The PR branch starts at main `4f7a8c0ead0c940b1e292a4c0dd1d52c7d1bb5f2`.
It contains only DETS implementation, required shared native/lifetime changes,
permanent fixtures and documentation. The mixed source checkout is preserved.
Main's newer bad-function admission and ordinary descriptor-polling path are
retained. No unrelated library port is included.

Baseline: OTP-29.1.1, commit `ad05823719d77c8faee87348ea39513d4e2f99c5`.
Original source/BEAM assets are unchanged. All64 explicit dets exports have
managed/original alternatives; unchanged dets_server/dets_sup remain shared
Erlang dependencies. See [backend selection](../../dets-backends.md).

| Check | Actual result | Evidence |
| --- | --- | --- |
| Isolated tests build |0 warnings/errors | Command below |
| DETS primary/upstream/magic groups |5/5 pass | [backends.json](backends.json) |
| Primary corpus |21 scenarios per backend, complete basic/live error stacks | [observations](observations/) |
| Unchanged upstream suite |7 cases per backend, all ok | [observations](observations/) |
| Connected GC/ETS/interpreter/raw file checks |9/9 pass | [connected.json](connected.json) |
| Real native v9 interchange |6/6 pass, including incremental growth | [interchange.json](interchange.json) |
| SDK layout |Full final layout check0 remaining | [layout-final.log](layout-final.log) |

The five DETS groups include the four-mode match-program lifetime group, which
also occurs in the connected report. Counts are grouped scenarios, not exhaustive
API certification. The observer projects owner PIDs to type checks and redacts
only its own dynamic file paths. Full captured reasons/stacks and effects remain
compared. Suite bodies and expected results are unchanged.

Commands from the isolated repository root:

```powershell
dotnet build tests/Erlang.Tests/Erlang.Tests.csproj -m:1 --artifacts-path artifacts/dets-pr-build
dotnet artifacts/dets-pr-build/bin/Erlang.Tests/debug/Erlang.Tests.dll --case runtime/dets/managed-public-otp-contracts --case runtime/dets/original-public-otp-contracts --case runtime/dets/managed-upstream-cases --case runtime/dets/original-upstream-cases --case runtime/dets/magic-reference-gc-contracts --report artifacts/dets-pr-validation/backends.json
dotnet build tools/Validation/Erlang.DetsCompatibility/Erlang.DetsCompatibility.csproj -m:1 --artifacts-path artifacts/dets-interchange-build
dotnet artifacts/dets-interchange-build/bin/Erlang.DetsCompatibility/debug/Erlang.DetsCompatibility.dll . <native-capture-directory> artifacts/dets-pr-validation/interchange
py -3 tools/Validation/inventory-dets-source.py --upstream <readonly-pinned-otp-checkout> --output artifacts/dets-pr-validation/source-inventory.json
```

Use `tools/validate-dets-original.ps1` with the admitted official provider to
obtain native input files. The interchange run reused the byte-bound original-v10
capture; its receipt includes those input hashes and native manifest identity.
The original five modules' source/BEAM hashes are in
[source-inventory.json](source-inventory.json). The older local acceptance,
including its failures, remains [historical](../dets-port-20261104/implementation/README.md).

Layout check: `dotnet artifacts/dets-pr-build/bin/Erlang.Style/debug/Erlang.Style.dll --check .`
identified four connected files. SDK formatting on an isolated root fixed those
four; scoped and subsequent full final layout checks both returned0. They contain only layout changes after the
5/5 and9/9 execution. A subsequent ordinary build has0 warnings/errors, and the same nine connected
checks pass again in [formatted-connected.json](formatted-connected.json).
[Final build identities](final-build-assemblies.json) and [source inputs](source-inputs.json)
are separate from the original tested identities. No result
is attributed to a different binary. No full regression or validate.ps1 ran.
Canonical readiness is preserved. The full upstream suite, exhaustive failure/
resource/scheduling cases, Linux/macOS and whole release-manifest startup remain
unverified. Finite Windows evidence does not prove universal100% compatibility.

No merge or review-thread resolution is part of this PR-creation request.
