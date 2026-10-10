# Durable project instructions

Code readability: put each statement on its own line, expand nontrivial control-flow blocks and separate logical stages with blank lines. Keep .editorconfig formatting consistent. Extract semantic values into files owned by their domain; choose names by meaning, not just literal value. Equal strings/numbers can represent different contracts and must remain separate (for example lexer token kinds versus bit segment types, BIF names versus exception classes, or default integer width versus bits per byte). Do not globally merge literals by text equality. Keep source-language test fixtures near their use. Compiler diagnostic codes and messages belong in the domain diagnostic catalog, including one-off messages; use named builders for dynamic messages and preserve text, formatting and offsets. Review each replacement in context.

Each namespace-level type (class, record, struct, interface, enum or delegate) must live in its own matching TypeName.cs file. Keep genuinely nested types inside their owning type. Top-level executable statements stay in Program.cs; helper types belong in separate files. Compiler diagnostic groups are separate types/files, not multiple classes in a shared catalog file.

Formatting: no blank lines at the start of a file; each enum member occupies its own line; switch expressions put each arm and both braces on separate lines; separate method declarations with a blank line. Separate return from preceding statements in the same block with a blank line. Wrap all parameters/arguments onto individual indented lines when a list contains four or more items, or multiple items whose combined token width exceeds 120 characters. Keep guard conditions attached to their returned statement. Use the SDK-backed C# layout tool: dotnet run --project tools/Erlang.Style -- --write .; --check is included in tools/validate.ps1. Keep C# line endings LF, as .gitattributes requires. Native-Erlang hybrid Program.cs stays under its dedicated compiler pipeline. Extract process/OTP protocol atoms and all literal exception messages/reasons into appropriate domain type files, including tool/ETF diagnostics; preserve distinct meanings for equal values.

Work only in this repository. The sibling `../otp` is the official reference checkout and must remain unchanged. Read `CHANGELOG.md`, `docs/PROGRESS.md`, `docs/NEXT_STEPS.md`, `docs/COMPATIBILITY.md`, `docs/DECISIONS.md` and `reference-baseline.json` before implementing the next work unit.

Baseline: OTP-29.1.1, commit ad05823719d77c8faee87348ea39513d4e2f99c5. Read reference files using `git -C ../otp show OTP-29.1.1:PATH`; its checked-out HEAD is newer. Never silently upgrade the baseline.

Implement runtime, compiler, tooling and OTP components in C#. Do not introduce a production BEAM dependency. PowerShell validation is test infrastructure only. Preserve Erlang semantics: immutable terms, exact versus numeric equality, signed floating zero, codepoint atom order, exact map keys, single assignment, selective mailbox scanning, signal ordering and fault propagation. Do not substitute actor-framework default semantics without a source/contract audit.

Before each component, inspect candidate .NET implementations and record source, license, evidence and reuse decisions in `docs/dependency-decisions.md`. New runtime/compiler semantics need meaningful permanent tests and reference comparisons when the development oracle is available. A passing local regression test does not establish full compatibility.

Commands: `dotnet build -m:1`; `dotnet run --project tests/Erlang.Tests --no-build -- artifacts/tests.json`; `pwsh -File tools/validate.ps1`. The harness deliberately does not depend on external test packages. Ordinary `dotnet build` also works outside sandbox worker-process restrictions. Validate generated examples and test failure when preprocessing is disabled.

Update progress, compatibility, semantic differences and next executable tasks at each checkpoint. Keep machine-readable statuses and tests consistent with human documentation. Never mark an entire OTP module verified when only some MFAs exist. Use the status vocabulary from the assignment. No completion claims until the full definition of done is satisfied. Do not fabricate oracle results or performance comparisons.

At each checkpoint regenerate docs/MODULE_READINESS.md and docs/module-readiness.json with the readiness CLI and the current passed test report. Update component milestones in docs/module-readiness-scope.json and explicitly choose the previous immutable test snapshot for the delta. Report per-module registered MFAs, changes and remaining scope to the user; API presence against pinned declarations is not semantic completion. Keep C# OTP facades separate from registered Erlang-module exports.

The user requires a detailed English changelog for every work part. Root CHANGELOG.md is a short volume index; detailed entries live in sequential docs/changelog/NNNN.md files. Before starting a new part, read the index and latest volume and ensure the previous part is recorded. At every checkpoint, including unfinished or blocked parts, append a sequentially numbered, dated English entry using docs/changelog-entry-template.md only to the last volume. Start the next volume before the combined file would exceed 32 KiB or 400 lines; keep each entry whole (an oversized entry gets its own volume). Closed volumes are immutable; put dated corrections in the active volume and update the root index on rollover. Links must be relative to the volume location. Record objective, changed behavior/files, fixes/tests, reference/reuse/license decisions, compatibility scope, commands/reports, actual results, unrun checks, limitations/blockers and next step. Distinguish historical evidence from checks run in that part. Update current-state documents alongside the journal. Completing a part never implies completing the full assignment.

The public license for original code has not been chosen by the owner. Preserve third-party notices; never publish packages as part of local validation. The user has authorized commits and publication of completed work parts to this repository's configured Git remote: after appropriate validation and documentation updates, commit only the completed part, push to origin and report its hash. This does not authorize public NuGet publication. Save immutable validation snapshots under docs/validation/part-NNN and link them from that part's changelog entry, alongside current reports. Do not overwrite or include unrelated work.

Numeric constants: wire tags/versions, field widths/offsets, domain limits and buffer tuning values belong in semantic owner types, each in its own file. Equal numbers do not imply equal contracts (ETF atom codepoint maximum versus small integer maximum, nesting depth versus tuple/big digit thresholds, float width versus port field width). Keep protocol values separate from implementation/resource limits. Plain loop indexes/increments and arithmetic identities can stay literal when their meaning is immediate; review every domain-specific replacement in context.

## Part 037 checkpoint

Local last/split implementation passes414 tests/full pipeline; registry58/lists15/91. Publish and dispatch pinned304-case oracle (271 expressions,28 modules,5 diagnostics) and inspect same-head CI414 before promoting new reference proof. Historical282/282 remains part036. Next audited member/2 exact equality and append/1 improper-tail boundaries, then grammar/OTP callback adapters/signals. Approximately5% overall, no whole-module promotion.

## Part 038 checkpoint

Published last/split head f20113c now passes pinned304/304 (271 expressions,28 modules,5 diagnostics), same-head414-test CI/full integration and strict provenance checks. Registry58/lists15/91; evidence-only delta0 against immutable part037 tests. Next implement audited lists:member/2 exact equality and append/1 tail/error contracts, then missing grammar and Erlang OTP callback adapters/signals. Resource/reduction/stack contracts and full suites remain unfinished; approximately5% overall. Preserve historical reports; no whole-module promotion.

## Part 039 checkpoint

Local append/1 and existing member/2 boundary increment passes442 tests/full integration. Publish and dispatch exact pinned330-case oracle:297 expressions,28 modules,5 diagnostics; inspect same-head CI442 and preserve strict source/hash provenance. Registry59/lists16/91; member/2 already existed and is not a new MFA. Historical304/304 belongs to part038. Next audited lists:duplicate/2 and flatten/1 nested/improper/error/resource contracts, then grammar/Erlang callback adapters/signals. Approximately5% overall; no whole-module promotion.

## Part 040 checkpoint

Published append/member head 605ef3d passes pinned330/330 (297 expressions,28 modules,5 diagnostics), same-head442-test CI/full integration and strict provenance. Registry59/lists16/91; evidence-only delta0 against immutable part039 tests. member/2 already existed; append/1 is the sole new MFA. Next audited lists:duplicate/2 and flatten/1 nested/improper/error/resource contracts, then grammar/Erlang callback adapters/signals. Approximately5% overall; no whole-module promotion. Preserve initial part039 signed-zero test expectation failure and all historical evidence.

## Part 041 checkpoint

Local duplicate/2 and flatten/1,2 pass472 tests/full integration; registry62/lists19/91. Publish and dispatch pinned355-case comparison:322 expressions,28 modules,5 diagnostics, same-head CI472 and strict source/hash provenance. Preserve297 old expressions and28 compiled cases. Huge positive duplicate limits are local-only resource policy, not live allocation oracle cases. Next substantive compiler feature: audited if expression guards/alternatives, no-match if_clause and branch binding/export rules through interpreted, compiled and hybrid paths; then Erlang OTP callback adapters/signals. Approximately5% overall; no whole-module promotion.

## Part 042 checkpoint

Published duplicate/flatten head 3c138b2 passes pinned355/355 (322 expressions,28 modules,5 diagnostics), same-head472 tests/full integration and strict provenance. Registry62/lists19/91; evidence-only delta0 against immutable part041 tests. Huge positive duplicate cap remains local-only resource policy. Next substantive compiler if expression guards/alternatives, no-match if_clause and branch binding/export rules through interpreted, compiled and hybrid paths; then Erlang OTP callback adapters/signals. Approximately5% overall, no whole-module promotion; preserve historical reports.

## Part 043 checkpoint

Constants-only refactor preserves319 values/five source token streams and322/28/5 corpus/generated hashes; full472 tests/integration pass.149 namespace-level types/no layout violations;62 MFAs/delta0, lists19/91, approximately5%. Shared language operators/erlang MFA spellings have Terms catalogs; local keywords/syntax/bit/variable/numeric/timeout/map/io/process names keep separate owners. Equal slash/minus/colon/BIF/exception meanings must not be merged. Historical355/355 remains part042 at3c138b2; no live oracle rerun here. Next audited compiler if feature across interpreted/compiled/hybrid paths.

## Part 044 checkpoint

Local if grammar/guards/if_clause/common binding export passes516/full integration. New corpus346 expressions/36 compiled modules/10 diagnostics=392; preserve322/28/5 old contracts. Publish and dispatch pinned oracle/same-head516 CI before promoting reference proof. Initial509/516 fixture-sequence failure preserved under part044. Registry62/delta0, lists19/91, approximately5%. Next remaining grammar begin/operators followed by Erlang callbacks/signals. Keep hybrid C# if detection and historical355/355 scope explicit.

## Part 045 checkpoint

Published if head c7e575b passes pinned392/392 (346 expressions,36 modules,10 diagnostics), same-head516/full CI and strict provenance.151 types/no violations,62 MFAs/delta0, lists19/91, approximately5%. Preserve322/28/5 historical contracts and initial part044 fixture-sequence failure. Next begin blocks/operators then Erlang callback adapters/signals; full grammar/annotation/warning/stacks/resources/reference suites incomplete. Part043 tests/integration snapshots reuse part041 reports; actual part043 execution is recorded in its validation log, clarified by dated active-volume correction.

## Part 046 checkpoint

Local begin/all ordinary operator spellings passes614/full pipeline;484-case plan419/48/17. Preserve346/36/10 prior contracts and generated hashes. Publish/dispatch pinned oracle and same-head614 CI.156 namespace types/no violations,62 MFAs/delta0, lists19/91, approximately5%. Catch logical stack terms are not full OTP frame/annotation equivalence; left-shift CLR result-bit indexing is local resource policy. Initial duplicate helper name build failure preserved. Next remaining construct grammar/OTP Erlang callback adapters; no whole-module promotion.

## Part 047 checkpoint

Published begin/ordinary operator head b7b8c72 passes pinned484/484 (419 expressions,48 modules,17 diagnostics), same-head614/full CI and strict source/hash/outcome provenance.156 types/no violations,62 MFAs/delta0, lists19/91, approximately5%. Catch logical stack frames are not full source/frame equivalence; left-shift CLR bit-index resource bound is local policy. Preserve346/36/10 history and initial part046 helper-name build failure. Next remaining construct grammar/Erlang OTP callback adapters/signals, no whole-module promotion.

## Source-directed port policy — Part 048

The user accepted close porting to reduce independent implementation drift. Before adding/replacing an algorithm, read the exact pinned original routines and applicable tests. Prefer original control flow, state transitions, evaluation/error order and structures; document every CLR adaptation and unported behavior in docs/OTP-PORTS.md. Preserve notices/SPDX/copyright and identify modifications for direct adaptations; include applicable notices/full license in packages. Existing independently implemented code is not retrospectively a port. Preserve regressions and compare interpreted plus compiled-reference behavior; matching a small corpus does not guarantee compatibility. Do not rewrite all working components at once.

Part048 local expression-list/cons binding port passes648/full integration; new corpus439/54/25=518, preserve419/48/17 history.158 types/no violations;62 MFAs/delta0, approximately5%. Publish and verify518/same-head648 CI. Next source-directed map binding scopes/bit callbacks, then typed lint states/grammar/OTP adapters. Preserve before-port629/641 failures including explicitly corrected unregistered-BIF fixture mistakes. Source-derived Apache notices are now present; original-code public license remains undecided.
