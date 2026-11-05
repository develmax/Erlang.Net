# code C# port — 2026-11-04

Implementation scope: all 69 pinned OTP 29.1.1 exports (62 C# library signatures
and seven shared interpreter-native signatures), with original Erlang/BEAM
retained. Original code_server, init/loader, helper libraries and purger remain
explicit dependencies. This is not an implementation of code_server in C#.

Read the [backend contract](../../code-backends.md) and
[API inventory](api-inventory.md). The inventory proves signature equality,
not universal OTP equivalence. The registered default MFA ledger is unchanged.

## CI and review follow-up

[Part579](../part-579/README.md) repairs the stale CI source plans and the reviewed
module_status error stacks. Both backends now match159 complete observations plus
the25-result lifecycle;17 focused groups and all passive admissions pass locally.
Earlier publication receipts below remain immutable evidence of their own head.
Hosted final-head CI completion is tracked separately.

## Initial PR publication over current main

The isolated branch starts at main `67809531b` and contains only the code port,
its integration, tests and documentation. The ordinary test-project build passes
with zero warnings/errors and no excluded modules. [All17 connected groups pass](publication/focused.json)
on the rebased source. Each backend again matches156 original observations and
the complete25-result real-BEAM lifecycle. The earlier working-tree reports below
are historical evidence of their own source snapshots, including the former
DETS compile filter; they do not describe the current publication build.

Public API names, code-server messages, dependency operations, monitor fields,
AST layouts, EEP48 metadata/version, source positions, closure identity widths,
messages and test budgets now have semantic owners. Equal strings retain separate
constants for different roles. The [constant-expansion audit](publication/literal-refactor-equivalence.json)
confirms token-equivalent behavior in all seven implementation units. The
[current inventory](publication/source-inventory.json) records69 signatures and
zero inline strings or unnamed numbers above the explicit small-arity range in
those units. [Publication source bindings](publication/validation-summary.json)
record the exact composed files; global readiness remains intentionally unchanged.

## Historical working-tree validation

- Both alternatives pass the same 156 original observations, comparing complete
  returned terms or exception class/reason/full stack.
- Both pass the complete 25-result real-BEAM lifecycle scenario: binary loading,
  direct is_loaded, which, execution, ensure, status, availability, chunk/MD5,
  repeated loading, purge/delete, opaque preparation, atomic publication and
  third-generation refusal. No prepared reference is substituted with expected
  code; the actual result is published and executed.
- All 17 connected test groups pass. The separate managed native-profile check
  and unchanged original seven-native-profile check both pass.
- Narrow runtime/managed/test builds have zero warnings/errors. The shared
  unfinished DETS draft was excluded only from the scoped managed-library build;
  its sources were not edited. This is not a clean whole-workspace build claim.

The actual partial report is [focused-final.json](focused-final.json).
The subsequent four-case provenance replay is preserved separately. Source/API,
corpus/provider and implementation hashes are in the adjacent inventories and
provenance files. Failed attempts remain in the raw archive; they are not silently
reclassified as passing.

## Reproduce

Run from the repository root:

```powershell
pwsh -NoProfile -File tools/validate-code-original.ps1 -Output artifacts/new-code-capture
py -3 tools/Validation/audit-code-source.py --output artifacts/new-code-audit
dotnet build src/Runtime/Erlang.Runtime.Core -m:1 --no-restore
dotnet build src/Otp/Managed/Erlang.Otp.Stdlib -m:1 --no-restore
dotnet build tests/Erlang.Tests -m:1 --no-restore
dotnet tests/Erlang.Tests/bin/Debug/net10.0/Erlang.Tests.dll --case otp/code/managed-contracts --case otp/code/beam-contracts --case otp/code/selection-refusals --case otp/code/managed-native-profile --report artifacts/new-code-focused.json
```

The current shared checkout also contains unrelated incomplete implementations.
The retained `exclude-unrelated.targets` records the scoped build filter used at
this checkpoint. Pass its absolute path as `CustomBeforeMicrosoftCommonTargets`
when reproducing that scoped build and use `BuildProjectReferences=false` after
building runtime dependencies. Do not infer readiness of excluded drafts.

The acquisition script validates the admitted official provider's execution
manifest and code/code_server/binary identities before and after capture.
Permanent embedded inputs are hash checked before replay. The original code
image is separately checked against its existing resource manifest.

## Reproduced defects and repairs

The lifecycle comparison exposed a pre-existing BEAM import shortcut returning
`host_beam` for direct `code:is_loaded/1` even when a real interface was selected.
Imports now invoke the registered backend. Existing on_load/bootstrap observer
groups confirm that the earlier interpreter-only bootstrap still operates.

Six initial C# errors omitted partition_load/do_prepare_loading source frames;
the exact pinned helper/caller frames and depth truncation are restored.
The first harness staged BEAM inside the publication writer and was correctly
refused; it now uses the established asynchronous fixture preparation scope.
Missing original purger dependencies initially terminated both backends' server;
the fixture now boots genuine erts_internal and erts_code_purger images.
The first oracle's malformed path also terminated stock OTP's server, so that
crash remains a separate preserved observation rather than poisoning the corpus.

## Remaining acceptance

Universal 100% compatibility is not certified. JIT coverage/debug capability
differs from this runtime's established non-BEAMASM profile. Full release startup
closure, all documentation/debug-AST and gzip dependency paths, filesystem and
platform failures, scheduling races, physical resource limits and the entire
upstream code_SUITE still require acceptance. No full regression, global
readiness regeneration, CI, commit/push/merge or package publication is claimed.
The user explicitly required implementation before tests and connected focused
checks instead of the complete regression; canonical readiness is preserved as
historical evidence rather than refreshed from a partial run.
