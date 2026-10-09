# Progress checkpoint — 2026-10-09

The **full work.md assignment is not complete**. Overall completion remains approximately **5%**, a coarse engineering estimate, not measured semantic compatibility. Most OTP services/exports, distribution, advanced runtime, full grammar, IDE/debugger work and reference-suite hardening remain unfinished. There is no reliable weighted contract denominator.

Reference: **OTP-29.1.1**, SHA ad05823719d77c8faee87348ea39513d4e2f99c5. SDK 10.0.400, C# 14, net10.0. Implementation is in this repository; the sibling reference checkout is unchanged. Production builds/execution need C#/.NET and do not require Erlang/BEAM. Detailed English history and immutable reports: [CHANGELOG](../CHANGELOG.md), parts 001–015.

| Work unit | Current deliverable | Status |
| --- | --- | --- |
| Discovery/bootstrap | 11 projects, pinned baseline/SDK, source inventory, durable instructions | Partially compatible |
| Terms/ETF | Immutable Erlang terms, exact/numeric equality/order/signed zero, common bounded ETF formats | Partially compatible |
| Runtime | Selective mailboxes, local names/dictionaries, links/monitors/exits, atomic spawn_monitor, iterative exit cascades | Partially compatible |
| Compiler | Lexer/Pratt AST, guard/scope analysis, clauses/closures, case/receive, maps, integer/binary/float/UTF bit syntax, generated C# AST/evaluator and module-local tail-call trampoline | Partially compatible |
| Core modules | 49 registered erlang/lists/maps/io MFAs, each with a direct permanent contract test | Partially compatible |
| OTP foundation | C# gen_server callbacks and supervisor strategies/policies/intensity/ordered shutdown | Partially compatible |
| Hybrid/build | Native receive/case/fun in async C#, Roslyn lexical context, line mapping, preprocessing/incremental/clean | Partially compatible |
| Local packages/tool | Real PackageReference consumer, local CLI installation and .erl compilation | Implemented |
| Validation/performance | Permanent harness, exact-version differential runner/CI and historical exploratory benchmark harness | Implemented infrastructure; full semantic/performance equivalence unverified |

Source inventory: **1,289 modules / 38,622 explicit source exports / 524 BIF declarations**. Conditional/macro/generated/NIF/platform expansion remains incomplete. These counts and the 49 partially compatible MFAs are not directly comparable API coverage measures.

## Current validation

- **299/299 local tests passed**, including 49 direct MFA cases, 63,488 finite binary16 roundtrips, 1,280 deterministic UTF scalar roundtrips and malformed/scalar/endian/unaligned/scope regressions. [Report](validation/part-015/tests.json).
- `pwsh -File tools/validate.ps1` passed: full build with zero warnings/errors; generated .erl/hybrid examples; unchanged-input timestamps; expected disabled-preprocessing failure; clean/rebuild; real local-package consumer and installed local CLI. Examples produced exactly Hello World and executed map/bit/float/UTF assertions. [Integration](validation/part-015/integration-results.json).
- An unavailable local oracle returns code 2 with an incomplete infrastructure-error report and zero executed cases. No reference runtime is installed on this host.
- Remote exact-version run **37952290309 at 19774f9** matched **123/123** values/exception class/reason cases, including 30 float additions. Actual artifact digest was verified. [Report and metadata](validation/part-014/float-remote-checkpoint.json). This excludes the newer UTF implementation.
- Windows CI **37952244708 at 19774f9** passed its 271-test/full-integration predecessor checkpoint. The **67ac674** UTF head passed Windows CI **37953727459**; its **154-case oracle run 37953748714** is in progress; the 31 UTF additions are not yet remote-verified.

Existing permanent tests also cover 50,000 module-local tail calls, a 10,000-process link cascade, 200 immediate-child spawn-monitor attempts, supervisor strategies/intensity and gen_server call/cast/info/stop/crash/timeout/init paths. Historical Release benchmarks are exploratory only; they were not rerun for float/UTF and do not establish BEAM performance equivalence.

Local packages/cache/consumer/tool and transient logs stay under ignored artifacts/bin/obj. No public NuGet publication. User-authorized completed parts were committed/pushed to origin/main: float **19774f9**, UTF **67ac674**. Earlier hashes/results remain in Git and the English changelog.

## Remaining work

No known local test failure. Part 015 fixes a pre-existing numeric-coercion discrepancy: shared Integer.TryToDouble rounds nearest-even and rejects conversion overflow before mixed arithmetic or integer /, preserving badarith even for huge denominators. Float segments/literals use the same audited conversion. Six new regressions pass; the corpus is now 160 cases and these six arithmetic additions need new-head comparison. [Semantic differences](semantic-differences.md), [compatibility](COMPATIBILITY.md), [matrix](compatibility-matrix.md) and [machine-readable evidence](compatibility.json) define the tested subset. Successful selected oracle cases do not establish full module/MFA compatibility, stack traces, signal ordering or side effects.

Next: finish the 154-case UTF oracle result, save exact-head evidence and fix any discrepancy; then implement non-UTF literal-string size modifiers. Broader grammar/scopes/errors, process signal races/resources/fairness, Erlang OTP callback adapters/services, distribution/storage/code loading and IDE support remain unfinished. [NEXT_STEPS](NEXT_STEPS.md) has executable follow-up tasks.
