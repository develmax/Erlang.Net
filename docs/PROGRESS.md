# Progress checkpoint — 2026-10-10

The **full work.md assignment is not complete**. Overall completion remains approximately **5%**, a coarse engineering estimate, not measured semantic compatibility. Most OTP services/exports, distribution, advanced runtime, full grammar, IDE/debugger work and reference-suite hardening remain unfinished. There is no reliable weighted contract denominator.

Reference: **OTP-29.1.1**, SHA ad05823719d77c8faee87348ea39513d4e2f99c5. SDK 10.0.400, C# 14, net10.0. Implementation is in this repository; the sibling reference checkout is unchanged. Production builds/execution need C#/.NET and do not require Erlang/BEAM. Detailed English history and immutable reports: [CHANGELOG](../CHANGELOG.md), parts 001–023.

| Work unit | Current deliverable | Status |
| --- | --- | --- |
| Discovery/bootstrap | 12 projects (including the development-only style tool), pinned baseline/SDK, source inventory, durable instructions | Partially compatible |
| Terms/ETF | Immutable Erlang terms, exact/numeric equality/order/signed zero, common bounded ETF formats | Partially compatible |
| Runtime | Selective mailboxes, local names/dictionaries, links/monitors/exits, atomic spawn_monitor, iterative exit cascades | Partially compatible |
| Compiler | Lexer/Pratt AST, guard/scope analysis, clauses/closures, case/receive, maps, integer/binary/float/UTF bit syntax, generated C# AST/evaluator and module-local tail-call trampoline | Partially compatible |
| Core modules | 49 registered erlang/lists/maps/io MFAs, each with a direct permanent contract test | Partially compatible |
| OTP foundation | C# gen_server callbacks and supervisor strategies/policies/intensity/ordered shutdown | Partially compatible |
| Hybrid/build | Native receive/case/fun in async C#, Roslyn lexical context, line mapping, preprocessing/incremental/clean | Partially compatible |
| Local packages/tool | Real PackageReference consumer, local CLI installation and .erl compilation | Implemented |
| Validation/performance | Permanent harness, exact-version differential runner/CI and historical exploratory benchmark harness | Implemented infrastructure; full semantic/performance equivalence unverified |

Source inventory: **1,289 modules / 38,622 explicit source exports / 524 BIF declarations**. Conditional/macro/generated/NIF/platform expansion remains incomplete. These counts and the 49 partially compatible MFAs are not directly comparable API coverage measures.

## Code readability checkpoint

Part 018 extracts reusable reason atoms, exception classes and bit-syntax values into domain-owned files, with separate names for equal literals that represent different contracts. It formats 21 existing C# files and separates logical stages in bit helpers/numeric conversion. .editorconfig and AGENTS.md preserve the convention for future parts. The source-token/value audit is in [part 018](validation/part-018/source-token-audit.json); this refactor adds no language feature or MFA and does not change the approximately 5% estimate. The 160-case reference result below remains evidence from its stated historical head; the oracle was not rerun for this refactor.

Part 019 completes compiler diagnostic extraction, including one-off texts and ten dynamic message builders, and adds separate lexer-kind, bit-category, alias/sign/unit constants. All CompileException codes/texts are owned by dedicated CompilerDiagnosticCodes and domain diagnostic types (separate files after part 020); literal test expectations remain independent. The [part-019 token audit](validation/part-019/source-token-audit.json) records 103 additional constant references and ten inlined builders with zero mismatches across four existing implementation files. Constant catalog definitions were reviewed separately. This continues the refactor without new semantic features.

Part 020 establishes one namespace-level type per matching TypeName.cs file throughout src/tools/tests/benchmarks/examples. The [layout audit](validation/part-020/layout-audit.json) preserves all 80 type declarations/namespaces and the two executable statement files, with zero token mismatches or layout violations after formatting. Thirteen original files were reorganized; nested types stay with their owners. tools/validate.ps1 now copies all root C# files of HelloHybrid into its real package consumer. No semantic feature or progress percentage change.

Part 021 splits detailed English history into indexed docs/changelog volumes with a 32 KiB / 400-line rollover policy. Process/OTP atoms, absence sentinels and remaining exception texts/reasons are domain-owned. The SDK-backed development-only Erlang.Style tool enforces enum/member/return/header/argument layout through tools/validate.ps1; four or more arguments, or a multi-item token width over 120, use separate indented lines. C# LF endings are enforced through .gitattributes. [Refactor audit](validation/part-021/source-token-audit.json) preserves 71 existing implementation/fixture token streams; [style probe](validation/part-021/style-probe.json) independently exercises failure/reformat/pass. No language/MFA feature or percentage change.

Part 022 replaces nontrivial ETF numeric literals with separate wire tag/header/field/atom/integer/tuple/bit/map/PID/reference/compression/resource groups, one type per file. [Pinned source audit](validation/part-022/reference-tags.json) confirms 27 tags and version 131; [token audit](validation/part-022/source-token-audit.json) preserves 97 extracted references. Depth 256, compressed buffer 4096 and the existing default byte cap remain implementation choices, not claimed OTP limits. Full local validation passed on retry; the [initial failure](validation/part-022/initial-validation.json) is preserved separately. No semantic feature/MFA or percentage change.

## Current validation

- **299/299 local tests passed**, including 49 direct MFA cases, 63,488 finite binary16 roundtrips, 1,280 deterministic UTF scalar roundtrips and malformed/scalar/endian/unaligned/scope regressions. [Report](validation/part-023/tests.json).
- `pwsh -File tools/validate.ps1` passed: full build with zero warnings/errors; generated .erl/hybrid examples; unchanged-input timestamps; expected disabled-preprocessing failure; clean/rebuild; real local-package consumer and installed local CLI. Examples produced exactly Hello World and executed map/bit/float/UTF assertions. [Integration](validation/part-023/integration-results.json).
- An unavailable local oracle returns code 2 with an incomplete infrastructure-error report and zero executed cases. No reference runtime is installed on this host.
- Remote exact-version run **37955525454 at 666c4b5** matched **160/160** values/exception class/reason cases: prior 93, float 30, UTF 31 and arithmetic-coercion 6. Version confirmed, complete report, zero failures/aborts; actual artifact digest verified. [Report and metadata](validation/part-017/remote-checkpoint.json). Reference expressions use erl_eval; compiled-reference-module/optimizer equivalence remains unverified.
- Same-head Windows CI **37955516789 at 666c4b5** passed **299/299 tests** and full build/integration/package validation. Its actual log confirms zero warnings/errors and Validation passed. Part 017 observes this remote evidence; no new runtime changes or local test rerun.

Existing permanent tests also cover 50,000 module-local tail calls, a 10,000-process link cascade, 200 immediate-child spawn-monitor attempts, supervisor strategies/intensity and gen_server call/cast/info/stop/crash/timeout/init paths. Historical Release benchmarks are exploratory only; they were not rerun for float/UTF and do not establish BEAM performance equivalence.

Local packages/cache/consumer/tool and transient logs stay under ignored artifacts/bin/obj. No public NuGet publication. User-authorized completed parts were committed/pushed to origin/main: float **19774f9**, UTF **67ac674**, shared numeric conversion **eb621ec**, UTF-size repair **666c4b5**. Earlier hashes/results remain in Git and the English changelog.

## Remaining work

No known local test failure. Part 016 rejects explicit undefined UTF sizes in construction/patterns after exact-reference erl_lint evidence; prior positive undefined tests/corpus were invalid and are corrected. Final local count stays 299. Part 015 fixes a pre-existing numeric-coercion discrepancy: shared Integer.TryToDouble rounds nearest-even and rejects conversion overflow before mixed arithmetic or integer /, preserving badarith even for huge denominators. Float segments/literals use the same audited conversion. Six new regressions pass; the corpus is now 160 cases and all six arithmetic additions match the pinned oracle in the 160-case report. [Semantic differences](semantic-differences.md), [compatibility](COMPATIBILITY.md), [matrix](compatibility-matrix.md) and [machine-readable evidence](compatibility.json) define the tested subset. Successful selected oracle cases do not establish full module/MFA compatibility, stack traces, signal ordering or side effects.

Next: implement construction-only non-UTF literal-string size modifiers with empty-string error validation and preserve pattern restrictions, then extend the oracle corpus. Broader grammar/scopes/errors, process signal races/resources/fairness, Erlang OTP callback adapters/services, distribution/storage/code loading and IDE support remain unfinished. [NEXT_STEPS](NEXT_STEPS.md) has executable follow-up tasks.

## Part 023 — Readable switch expressions

Switch expressions place the opening brace, each arm and the closing brace on separate lines. The SDK-backed style tool enforces this throughout ordinary C# sources and full validation checks it. TermOrder.Rank, Parser.Precedence and Semantics.Variables preserve their exact non-trivia token streams, values and pattern order. The independent guarded-pattern probe rejects compact layout, repairs it and passes a second check while preserving a string containing switch-like text. No new Erlang feature, MFA, compatibility promotion or dependency. Current 299-test/full integration reports are saved in validation/part-023; the 160-case oracle evidence remains historical at 666c4b5 and was not rerun.
