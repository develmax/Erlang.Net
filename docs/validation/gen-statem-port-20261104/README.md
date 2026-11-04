# gen_statem alternatives and semantic constants

This is the scoped publication of the user-requested gen_statem port and removal
of magic strings/numbers. The port was not on main. The PR contains that module,
minimal explicit startup selection, a related BEAM badfun frame correction and
focused evidence. It excludes the other local library drafts. The final source
is based on main91c05943d, including the newly merged code backend.

## Results and scope

Both explicitly selected backends pass **74/74 complete API observations and
311/311 engine scenarios** against a fresh admitted original OTP-29.1.1 capture.
The 256-case callback mode/event matrix is included in those 311 scenarios.
Full exception terms/stacks are compared without removing repeated frames.
The source audit confirms exact retained upstream source bytes and forty exports.
The original BEAM SHA256 is recorded alongside the actual managed/runtime assembly
hashes. The two reports name the same final assemblies and source bytes.

The ordinary Release builds of the runner and tests pass with zero warnings and
zero errors, without source exclusions. Exactly four connected function-call
tests pass; **the remaining 9712 registered tests were not run**. Their partial
report cannot be used as full regression/readiness evidence. Existing Resource529
admission passes4218 declared dependencies; all13 passive workflow source plans,
current MFA/native source bindings and22 CI-reuse controls pass. These passive
checks do not execute original/native test groups. No manual CI dispatch occurs.

Finite Windows comparisons do not certify100% OTP compatibility. Full upstream
gen_statem_SUITE, resources/owned heaps, cross-platform/distributed/global/via,
structured Logger handler effects and metadata parity remain separate gates.
Primary Logger output is disabled in this fixture; formatting and errors are
compared separately. Global readiness/classifications are not regenerated.
No live backend migration, automatic fallback, package publication or merge.

## Semantic ownership

Product catalogs distinguish process atoms, functions, dependency modules,
status keys/labels and log directives. Pinned source coordinates/private arities,
sys record positions, receive-after bounds and the client stack display limit
are named. Delegated request source lines are explicit per MFA, not inferred by
subtracting numbers from error-clause lines. Obsolete mechanical aliases and
format names such as Value/P2 are removed. Validator paths, provenance schema,
reflection, boot protocol, deadlines and corpus dimensions have separate owners.
Structural API signatures/tuple patterns and deliberate invalid observer values
remain explicit. The original Erlang observer sources and their line numbers
are unchanged. [Literal review](literal-review.json) records the local audit.

## Commands

Run from the repository root, with the installed Python3 executable if `python`
is unavailable. The admitted provider profile is machine-local; the capture
script checks its inventory before and after execution and refuses existing output.

```powershell
pwsh -File tools/validate-gen-statem-original.ps1 -Admission <official-admission.json> -Output artifacts/semantic-refactor/oracle
dotnet build tests/Erlang.Tests -c Release --nologo -m:1 -nr:false -v q
dotnet build tools/Validation/Erlang.GenStatemCompatibility -c Release --nologo -m:1 -nr:false -v q
dotnet tests/Erlang.Tests/bin/Release/net10.0/Erlang.Tests.dll --case runtime/beam-fun/tail-rejection-retains-source-caller --case runtime/beam-fun/tail-return-restores-local-continuation --case runtime/beam-try/async-host-erlang-exception-caught --case runtime/beam/lists-opcode-edges/dynamic-fun-arguments-and-continuation --report artifacts/semantic-refactor/rebased-fun-tests.json
dotnet tools/Validation/Erlang.GenStatemCompatibility/bin/Release/net10.0/Erlang.GenStatemCompatibility.dll . artifacts/semantic-refactor/oracle Managed
dotnet tools/Validation/Erlang.GenStatemCompatibility/bin/Release/net10.0/Erlang.GenStatemCompatibility.dll . artifacts/semantic-refactor/oracle Beam
python -B tools/Validation/audit-gen-statem-source.py artifacts/semantic-refactor/source-audit.json <read-only-otp-reference>
python -B tools/Validation/summarize-gen-statem.py . artifacts/semantic-refactor/oracle artifacts/semantic-refactor/source-audit.json artifacts/semantic-refactor/acceptance.json
python -B tools/Native/Erlang.NifResource529Observations/verify-source-plan.py --plan-sha256 <current-plan-sha256> --output artifacts/semantic-refactor/resource-source-plan.json
python -B tools/Build/Erlang.Tool/verify-mfa-source-bindings.py --repository . --output artifacts/semantic-refactor/mfa-source-bindings.json
python -B -m unittest discover -s tools/Validation/Erlang.CiReuse -p 'test_*.py'
```

The13-plan passive checker is extracted verbatim from the current validate.yml
step; its exact text and logs are in diagnostics.zip. The repository SDK-backed
Erlang.Style implementation formats/checks only package files in a staging tree.
Changed Markdown link targets are checked against the repository tree separately.

## Retained evidence and failures

- [Acceptance](acceptance.json), [Managed](Managed-report.json), [Beam](Beam-report.json)
  and [source audit](source-audit.json) bind the final comparisons.
- [Original provenance](provenance.json) and [raw oracle inputs](original-inputs.zip)
  retain the independent VM identity, corpora, feature bootstrap, exports and
  compiled observers.
- [Four connected tests](rebased-fun-tests.json), [Resource529](resource-source-plan.json),
  [13 plans](passive-source-plans.json) and [MFA/native bindings](mfa-source-bindings.json)
  retain the bounded checks.
- [Delivery inputs](delivery-inputs.json), [CI identity refresh](source-plan-refresh/refresh-audit.json)
  and [semantic scope proof](source-plan-refresh/semantic-scope.json) preserve
  existing plan membership, statuses, execution flags and workflow logic/triggers.
  Only source digests/byte counts, existing producer pins and the monitor freeze
  successor change. The monitor's previous path/hash remain its parent identity.
- [Diagnostics archive](diagnostics.zip) retains successful logs and earlier
  failures. Initial clean-main Beam replay was73/74 because badfun lost the
  suspended caller frame; this is fixed and the entire focused corpus passes.
  An earlier build collided with a running validator's loaded DLLs; those
  observations are not final assembly acceptance. Sparse-checkout test/admission
  attempts initially lacked unchanged fixtures; exact tracked inputs were
  materialized before successful reruns. The broad historical documentation
  guard could not run in the sparse checkout; only package target checks are
  claimed. Reports before the main rebase remain historical evidence.

Required hosted checks and review remain PR gates. No earlier-head success is
claimed as final-head CI completion. See [backend contract](../../gen-statem-backends.md)
and [architecture decision](../../decisions/0007-gen-statem-alternatives.md).
