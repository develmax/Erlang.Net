## Current batching and validation policy — 2026-11-03

Follow [Batched validation and delivery policy](docs/engineering/BATCHED_VALIDATION_POLICY.md): connected delivery packages with 60% new APIs/capabilities including full validation and 40% existing-contract closure, declared dependency-focused iteration checks and one complete final package gate. Target four or five measured independent full groups and approximately 20 minutes end to end; enforce exact coverage, bounded resources and isolated/exclusive lanes. Shared CI/helper changes and dispatch remain with the existing CI owner (#102, w-docs); root owns runtime integration and its separate focused poll experiment. Existing required workflows remain mandatory until reviewed replacements are active. Post-merge reuse requires explicit whole-input/matrix/review equivalence proof, never a matching SHA or DLL alone. No unchanged retry without proven transient cause. Preserve full public pinned-OTP GC/scheduler/resource contracts, raw failures and registry classifications. Historical checkpoints below retain their original scopes.

# Project instructions

## Layout

Use src/{Runtime,Compiler,Integration,Otp/Managed,Otp/Original/<category>} and tools/{Build,Validation,Native,Distribution,Collections,Compiler,Runtime}. Test directories and root tools/*.ps1 entry points stay in place. See [project layout](docs/project-layout.md). Historical paths must not recreate flat project directories.

Review and merge gate:
- Read PR conversation comments, review submissions and inline threads before merging.
- Pin the current head SHA and triage every finding against current source. An incomplete, failed or stale automatic review is not a clean review.
- Reproduce actionable defects, verify fixes and re-fetch comments after the final commit and CI.
- Never resolve a finding solely because its PR has been merged or its original line moved.

## PR lifecycle

- Within the user-authorized publication and merge scope, do not accumulate completed pull requests: after required checks pass on the current head and all current review findings have been triaged and fixed where applicable, merge the completed PR into main.
- Re-fetch the current PR head, comments, reviews and required checks after the final change. Stale or incomplete review summaries and earlier-head green CI do not establish final-head approval.
- Before deleting a working branch, verify its fresh SHA, main reachability or explicitly reviewed content equivalence, absence of useful unique commits and any remaining open-PR dependencies. Preserve dirty worktrees and recoverable source/history; use expected-SHA leases for remote deletion. Never delete main.
- After a verified merge, delete the merged local and remote working branch once the listed safety gates pass, then continue the next authorized work. If blocked, report the exact remaining gate. Never delete unmerged useful work; deleting a redundant non-ancestor branch requires explicit user authorization and reviewed content-equivalence/unique-commit proof.
- Unfinished work, unresolved actionable findings or failed/pending required checks remain explicit gates. This project workflow does not grant global approvals or override user authorization, branch protections or repository safety checks.

## Architecture and precedence

**Explicit supervisor exception:** the user requested a complete C# port of `supervisor` with the original Erlang backend still selectable. This authorizes the opt-in managed library bodies for `supervisor`. Preserve the pinned Erlang source, BEAM and explicit startup selection. This exception applies only to `supervisor`.

**Explicit io exception:** the user requested a complete C# port of io with the original Erlang backend still selectable. This authorizes the opt-in managed io library bodies for this module. Preserve the pinned Erlang source, BEAM and explicit startup selection. The exception applies to io.

**Explicit sys exception:** the user requested a complete C# port of sys with the original Erlang backend still selectable. This authorizes the opt-in managed sys library and preserves the pinned Erlang source, BEAM and startup backend selection. The exception applies to sys.

**Explicit erts_internal exception:** the user requested a complete C# port with a selectable original Erlang implementation. This authorizes the managed erts_internal library bodies for this module. Preserve the pinned Erlang source and BEAM and the explicit backend selection. This exception is scoped to erts_internal.

**Explicit ETS exception:** the user requested a complete C# port of ETS while retaining the selectable original Erlang implementation. This authorizes the ETS managed library layer (including folds, persistence control flow, QLC and browsing) in addition to shared runtime primitives. Preserve unchanged pinned Erlang/BEAM assets and explicit backend selection. This scoped instruction takes precedence over the general pure-module policy below and does not authorize unrelated module ports.

The user explicitly confirmed: "OTP-модули остаются на Erlang; C# — runtime/BIF". This later instruction supersedes the earlier work.md direction to manually translate pure Erlang OTP-library logic into C#. Do not silently rewrite the historical work.md or describe the previous work as upstream Erlang execution.

Keep pinned upstream pure Erlang modules as Erlang sources. Audit original Erlang -> BEAM -> our .NET runtime as the upstream alternative, while retaining our source compiler path. C# implements runtime semantics, native BIF/NIF primitives and host integration. Executing a BEAM asset in our runtime does not mean depending on the original ERTS VM in production. An original compiler can be a build-time dependency; self-hosting that compiler is a separate unproven milestone. Original OTP remains the development oracle.

For new pure-module work, first inventory the unchanged upstream artifact's actual instructions/imports and missing shared runtime capabilities. Distinguish BEAM loader/interpreter/native-binding gates from source parser/epp/compiler gates: documentation syntax does not block an already compiled BEAM. Then prove upstream identity -> selected backend -> actual .NET execution and compare values/errors/full stacks/effects against pinned OTP. Do not replace a compiler/loader gap with another handwritten C# implementation of that pure module. Source metadata admission must not silently ignore functional compile options, macros, imports or declarations.

Retain existing handwritten C# library implementations as supported alternatives, alongside original Erlang compiled to BEAM and executed by our .NET runtime. Plan explicit per-module backend selection under the same MFA API; record backend and source/BEAM/assembly identity in diagnostics and validation provenance. Do not silently fall back from BEAM to a C# library wrapper. Native BIFs remain runtime primitives, not a second library backend. Generated C#/IL from our Erlang compiler is a third execution path, not a manual port or BEAM execution. Use the same contract cases against pinned OTP for both alternatives, but retain separate acceptance. Do not assume a performance advantage without measurements after parity. No live hot-switch/state migration, automatic C# retirement, deletion or binding switch is authorized by this audit. Mixed modules such as binary retain explicit native primitive bindings. Registered API counts and current-backend classifications do not prove upstream execution.

Root is the sole owner of changes to AGENTS.md, WORK_PLAN.md and API_DELIVERY_PACKAGES.md, shared runtime/compiler/loader integration, builds and CI/publication. Helpers return isolated proposals/reviews. Independent CI and runtime checks continue. Preserve current evidence and prior failures; no broad rewrite or new publication/merge is authorized solely by this architecture clarification.

Historical checkpoint permissions and prohibitions apply only to their recorded work parts. Superseded architecture wording is preserved in the history rather than used as a current instruction.

## Durable development rules

Code readability: put each statement on its own line, expand nontrivial control-flow blocks and separate logical stages with blank lines. Keep .editorconfig formatting consistent. Extract semantic values into files owned by their domain; choose names by meaning, not just literal value. Equal strings/numbers can represent different contracts and must remain separate (for example lexer token kinds versus bit segment types, BIF names versus exception classes, or default integer width versus bits per byte). Do not globally merge literals by text equality. Keep source-language test fixtures near their use. Compiler diagnostic codes and messages belong in the domain diagnostic catalog, including one-off messages; use named builders for dynamic messages and preserve text, formatting and offsets. Review each replacement in context.

Each namespace-level type (class, record, struct, interface, enum or delegate) must live in its own matching TypeName.cs file. Keep genuinely nested types inside their owning type. Top-level executable statements stay in Program.cs; helper types belong in separate files. Compiler diagnostic groups are separate types/files, not multiple classes in a shared catalog file.

Formatting: no blank lines at the start of a file; each enum member occupies its own line; switch expressions put each arm and both braces on separate lines; separate method declarations with a blank line. Separate return from preceding statements in the same block with a blank line. Wrap all parameters/arguments onto individual indented lines when a list contains four or more items, or multiple items whose combined token width exceeds 120 characters. Keep guard conditions attached to their returned statement. Use the SDK-backed C# layout tool: dotnet run --project tools/Build/Erlang.Style -- --write .; --check is included in tools/validate.ps1. Keep C# line endings LF, as .gitattributes requires. Native-Erlang hybrid Program.cs stays under its dedicated compiler pipeline. Extract process/OTP protocol atoms and all literal exception messages/reasons into appropriate domain type files, including tool/ETF diagnostics; preserve distinct meanings for equal values.

Work only in this repository. The sibling `../otp` is the official reference checkout and must remain unchanged. Read `CHANGELOG.md`, `docs/PROGRESS.md`, `docs/NEXT_STEPS.md`, `docs/COMPATIBILITY.md`, `docs/DECISIONS.md` and `reference-baseline.json` before implementing the next work unit.

Baseline: OTP-29.1.1, commit ad05823719d77c8faee87348ea39513d4e2f99c5. Read reference files using `git -C ../otp show OTP-29.1.1:PATH`; its checked-out HEAD is newer. Never silently upgrade the baseline.

Implement runtime, native primitives, compiler, tooling and host integration in C#. Keep pure OTP modules in Erlang under the architecture policy above. PowerShell validation is test infrastructure only. Preserve Erlang semantics: immutable terms, exact versus numeric equality, signed floating zero, codepoint atom order, exact map keys, single assignment, selective mailbox scanning, signal ordering and fault propagation. Do not substitute actor-framework default semantics without a source/contract audit.

Before each component, inspect candidate .NET implementations and record source, license, evidence and reuse decisions in `docs/dependency-decisions.md`. New runtime/compiler semantics need meaningful permanent tests and reference comparisons when the development oracle is available. A passing local regression test does not establish full compatibility.

Commands: `dotnet build -m:1`; `dotnet run --project tests/Erlang.Tests --no-build -- artifacts/tests.json`; `pwsh -File tools/validate.ps1`. The harness deliberately does not depend on external test packages. Ordinary `dotnet build` also works outside sandbox worker-process restrictions. Validate generated examples and test failure when preprocessing is disabled.

Update progress, compatibility, semantic differences and next executable tasks at each checkpoint. Keep machine-readable statuses and tests consistent with human documentation. Never mark an entire OTP module verified when only some MFAs exist. Use the status vocabulary from the assignment. No completion claims until the full definition of done is satisfied. Do not fabricate oracle results or performance comparisons.

At each checkpoint regenerate docs/MODULE_READINESS.md and docs/module-readiness.json with the readiness CLI and the current passed test report. Update component milestones in docs/module-readiness-scope.json and explicitly choose the previous immutable test snapshot for the delta. Report per-module registered MFAs, changes and remaining scope to the user; API presence against pinned declarations is not semantic completion. Keep C# OTP facades separate from registered Erlang-module exports.

The user requires a detailed English changelog for every work part. Root CHANGELOG.md is a short volume index; detailed entries live in sequential docs/changelog/NNNN.md files. Before starting a new part, read the index and latest volume and ensure the previous part is recorded. At every checkpoint, including unfinished or blocked parts, append a sequentially numbered, dated English entry using docs/changelog-entry-template.md only to the last volume. Start the next volume before the combined file would exceed 32 KiB or 400 lines; keep each entry whole (an oversized entry gets its own volume). Closed volumes are immutable; put dated corrections in the active volume and update the root index on rollover. Links must be relative to the volume location. Record objective, changed behavior/files, fixes/tests, reference/reuse/license decisions, compatibility scope, commands/reports, actual results, unrun checks, limitations/blockers and next step. Distinguish historical evidence from checks run in that part. Update current-state documents alongside the journal. Completing a part never implies completing the full assignment.

The public license for original code has not been chosen by the owner. Preserve third-party notices; never publish packages as part of local validation. The user has authorized commits and publication of completed work parts to this repository's configured Git remote: after appropriate validation and documentation updates, commit only the completed part, push to origin and report its hash. This does not authorize public NuGet publication. Save immutable validation snapshots under docs/validation/part-NNN and link them from that part's changelog entry, alongside current reports. Do not overwrite or include unrelated work.

Numeric constants: wire tags/versions, field widths/offsets, domain limits and buffer tuning values belong in semantic owner types, each in its own file. Equal numbers do not imply equal contracts (ETF atom codepoint maximum versus small integer maximum, nesting depth versus tuple/big digit thresholds, float width versus port field width). Keep protocol values separate from implementation/resource limits. Plain loop indexes/increments and arithmetic identities can stay literal when their meaning is immediate; review every domain-specific replacement in context.

## Documentation maintenance

Keep current status in [PROGRESS](docs/PROGRESS.md), executable tasks in [NEXT_STEPS](docs/NEXT_STEPS.md) and detailed plans in [remaining work](docs/remaining-work/INDEX.md). Record checkpoint narratives in the active changelog volume rather than appending them to these entry pages or AGENTS.md.

Third-party license texts are in licenses/. The owner-local licenses/LICENSE.local.txt must remain uncommitted. Retain upstream source notices and package license names.

[Historical instructions and checkpoints](docs/history/agents/index.md).
