# Part580: selectable gen_event and semantic literal ownership

The scoped implementation is built from main `67809531bb2603202937ca0562e9dcf54174e298`.
The dirty integration checkout, unrelated module ports and historical build exclusions
are not prerequisites. Original Erlang sources and BEAM remain unchanged. C# exposes
all 44 non-metadata exports through explicit startup selection; default registration
is unchanged. This is finite evidence, not certification of 100% OTP compatibility.

## Implementation and refactoring

The port includes manager lifecycle, handlers/supervision, callback failures/swaps,
request collections, hibernation, system/state/code-change and logging callbacks.
The connected interpreter send failure path captures the Erlang send frame.
Semantic owner files distinguish API names, callback names, protocol tags, errors,
status/log fields, OTP source positions, packet layouts, limits and validation budgets.
Equal literals with different meanings retain separate owners. Ordinary small tuple
arities/indices and loop identities remain structural syntax. No blanket textual
replacement or timeout/expected-result normalization is used.

## Reproduction and final-source evidence

Run from the repository root (PowerShell; admitted OTP provider required):

```powershell
dotnet build tools/Validation/Erlang.GenEventCompatibility -m:1 --artifacts-path artifacts/gen-event579-build
py -3 tools/Validation/audit-gen-event-literals.py . --output artifacts/gen-event579-literals.json
pwsh tools/validate-gen-event-original.ps1 -Admission <official-admission.json> -Output artifacts/gen-event579-new -RunnerDll artifacts/gen-event579-build/bin/Erlang.GenEventCompatibility/debug/Erlang.GenEventCompatibility.dll
pwsh tools/Validation/summarize-gen-event-audit.ps1 -Directory artifacts/gen-event579-new -RunnerDirectory artifacts/gen-event579-build/bin/Erlang.GenEventCompatibility/debug
```

The ordinary focused build passes with zero warnings/errors. SDK style on owned C#
passes; the permanent literal audit passes on all nine logic files. Deliberately
injected inline string and numeric limit fail the audit; incomplete reports are
refused (see `negative-controls.json`). Source/assembly/provenance hashes are retained
in each report. Reports bind to the clean source tree; historical absolute paths are
provenance and need not exist on another machine. Original oracle is pinned to
OTP-29.1.1 / ad05823719d77c8faee87348ea39513d4e2f99c5.

Primary: Managed105/105 and Beam105/105, both admitted against current sources and
execution assemblies. The original init/code_server run on .NET for dependencies;
existing native storage/file/logger host integration is explicit. Actual export
source owners are checked; original mode has no managed gen_event fallback.

Depth0,1,2,8,32: both105/105. Depth3: Managed104/105, Beam105/105. The independent
Beam depth3 run continued after the acquisition wrapper stopped on the Managed
failure. `upstream_call` failed at unchanged upstream gen_event_SUITE.erl:853:
the response was already `{reply,{ok,hejhopp}}` when a zero-timeout check expected
`timeout`. The failure is retained, not retried away, ignored or reported as a pass.
Scheduling equivalence and the depth3 Managed acceptance gate remain open.

Ten unchanged upstream testcase functions are included; the complete Common Test
suite, whole repository regression, canonical readiness refresh, Linux/macOS,
distribution, resource exhaustion and performance certification were not run.
The user requested only related checks after implementation. No global readiness
or whole-module compatibility promotion is made.

## Publication and next gate

Publish only this scoped implementation/refactoring and its evidence as a PR to main.
Review the retained zero-timeout scheduling failure before merge. Preserve unrelated
dirty work and all historical evidence. No merge or package publication is requested.

## Main reconciliation

Main advanced to `91c05943d4dba4c912a76f80ac5e6d778bffe4b3` while the initial checks ran. Preserve
the preceding reports as pre-reconciliation evidence. The isolated tree now retains
the merged code backend and its source/CI data unchanged; fresh focused acceptance
is recorded separately under `rebased/`. The closed0104 journal is untouched.

Reconciled source build: zero warnings/errors; fresh primary Managed105/105 and
Beam105/105, both admitted against current sources and newly built assemblies.
`rebased/` contains the publication acceptance reports/corpus/provenance. Earlier
depth profiles belong to the pre-reconciliation build; they are historical checks,
including the retained Managed depth3 failure. No new all-profile pass is claimed.
Whitespace checking passes for authored changes; unchanged upstream Reference
fixtures intentionally retain their original trailing spaces and are excluded.
