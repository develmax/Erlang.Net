# Next executable tasks

Read PROGRESS and semantic-differences first; preserve existing tests/sample/package integration.

1. **Extend exact-version oracle comparison.** Current run 38012731419 at b13b56a matched **249/249 expressions + 14/14 compiled modules** against OTP-29.1.1; original reports/metadata are in validation/part-032. Initial abort/fix is preserved in part-025; earlier 160-case success remains historical in part-017. Local executable remains absent. Keep this corpus, add cases with each component, dispatch/inspect each tested head and preserve actual reports. Reference tracks now separately use erl_eval:exprs and compile:forms/code:load_binary versus generated C# assemblies. Expand selected compiled cases before broader compiler/optimizer claims; stacks, asynchronous signals and side effects also need separate tracks. Do not change baseline or introduce production BEAM.
2. **Expand bit syntax and map scopes.** Selected integer/binary/float/UTF construction/patterns, size/map BIFs, fun capture and numeric coercion cases match the current oracle. Integer/float literal-string construction is now implemented with empty-string validation; the complete 249-case corpus now passes; harden construction evaluation/error ordering and resource boundaries next. Pinned erl_lint good_string_size_type permits only default or unsized UTF strings in patterns; preserve those diagnostics. Continue scopes/map error precedence and full .erl/hybrid/PackageReference validation. Full MFA inputs/stacks/side effects/reference suites remain partial.
3. **Harden local signal and termination semantics.** Iterative link termination and atomic spawn_monitor now exist with 10,000-process cascade and immediate-child-exit tests. Add deterministic race tests for demonitor flush, links before spawn completion, trap_exit transitions and shutdown during receive. Make process-owned resources/callback cleanup ownership explicit. Measure reduction fairness under a CPU-bound compiled loop and suspended processes.
4. **Complete basic OTP callback integration.** Implement Erlang module callback adapters for gen_server and dynamic supervisor children. Add sys and alias-based late-reply suppression, validate child specs/restart/shutdown edge cases against the oracle. Current behaviour APIs are a C# subset only.
5. **Expand inventory contracts.** Keep git archive pinning; add conditional/macro expansion and callback name/arity/type signatures, generated/NIF exports and runtime/public OS contracts. Finish source/license/maintenance audits for transport candidates before reuse. Reconcile the machine-readable supported-MFA overlay with effective exports.
6. **Improve diagnostics/hybrid scopes.** Attach source spans to every AST/pattern node, support expression columns and debug mappings, explicitly diagnose sync receives, and design cross-block Erlang bindings/C# conversions. Preserve ordinary C# raw/interpolated strings, comments, switch syntax, generics and attributes.
7. **Expand runtime/standard-library scope.** Timers/flags/options, storage, gen_statem/gen_event/application, then distribution and advanced services. Every new MFA needs reference-backed input/error/side-effect cases and an updated registry.

Resume commands:

```powershell
dotnet build -m:1
pwsh -File tools/validate.ps1
dotnet tools/Erlang.Tool/bin/Debug/net10.0/Erlang.Tool.dll inventory ../otp docs
dotnet run --project benchmarks/Erlang.Benchmarks -c Release -- artifacts/benchmarks.json
```

The validation script installs its tool only under artifacts/local-tool and writes a consumer only under artifacts/package-consumer. On repeat runs it uninstalls/reinstalls that local tool and uses content-hash-specific workspace caches to validate current package bytes. Do not install globally. Package restore stays within the workspace-local feed/cache. No public NuGet publication or Git changes to the reference checkout.

Carry part 018 readability forward: use domain-owned constants only when meaning is shared, retain distinct constants for equal values with different contracts, and separate logical stages with blank lines. Preserve source-language fixtures verbatim when formatting C#. The feature sequence above is unchanged.

Part 019 clarification: put compiler diagnostic codes and all messages, including one-off texts, in their corresponding diagnostic type files; use domain builders for interpolated messages. Continue LexerTokenKinds and bit-specific groups without merging equal strings across meaning. Keep independent literal expectations in test fixtures.

Part 020 file organization: put every namespace-level class/record/struct/interface/enum/delegate in its own matching TypeName.cs file. Keep nested declarations inside their owner and top-level executable statements in Program.cs. Preserve all HelloHybrid root C# files when building its external package consumer. Earlier composite compiler files have been replaced by Parser.cs, Lexer.cs, Expr.cs, CompilerDiagnosticCodes.cs and separate domain diagnostic/constant files.

Part 021 conventions are enforced by tools/Erlang.Style in the full validation pipeline. Before committing source changes, run its --write command and validate; preserve domain-owned process/OTP constants and exception diagnostics. Read root CHANGELOG.md plus only the latest journal volume for current handoff. Append detailed English entries to that last volume; create the next before 32 KiB or 400 lines and update the index. The semantic feature sequence above is unchanged.

Part 022: use EtfTags and the separate ETF layout/limit/tuning types when extending serialization. Keep wire values distinct from implementation caps and equal numeric values distinct by contract; preserve independent literal ETF reference fixtures. The implementation-only nesting/byte/reference limits remain narrower than full OTP coverage. Continue the prior semantic feature sequence after this refactor.


Part 023 checkpoint: readable switch expressions are enforced by Erlang.Style and validate.ps1. Preserve one arm per line during the next planned language/runtime increment; current tests/integration are in validation/part-023. Full-assignment estimate remains approximately 5%.

## Part 027 checkpoint

Next execute the **219-case** exact-version workflow against the published lists/readiness head and preserve complete same-head oracle/Windows CI artifacts. Do not infer the new lists contracts from historical 183/183. Retain the existing compiled-reference-module/error-ordering/signal/OTP adapter tasks above. For the next standard-library increment, inspect lists reverse/2 and key-search contracts before implementation. Regenerate MODULE_READINESS.md/module-readiness.json at every checkpoint, update delivered/remaining scope, and choose the previous immutable snapshot explicitly for meaningful deltas. Current local tests: **329**, registry: **53**, lists: **10 (+4)**. Full assignment remains approximately 5%.

## Part 028 checkpoint

The 219-case oracle and same-head Windows CI are complete and preserved. Next: a separate compiled-reference-module comparison track and the audited lists reverse/2/key-search increment; keep all 219 expression cases. Continue signal races and Erlang OTP callback adapters as separate substantive work units. Current module report remains 53 MFAs, lists10 (+4 versus part-026), local/CI tests329; resource/stacks/full suites and the broad assignment remain unfinished.

## Part 029 checkpoint

Publish the locally validated key-search/reverse increment and dispatch the complete **249-case** oracle on that exact head. Preserve raw same-head oracle/Windows CI results and corpus 219+30 provenance before claiming selected contracts verified. Current tests 344; registry 56; lists 13/91 (+3 versus part-028). Then add the separate compiled-reference-module track and audit further lists contracts; full signal/OTP callback/grammar tasks remain.

## Part 030 checkpoint

The 249-case oracle and same-head Windows CI are complete and preserved. Next implement a separately compiled-reference-module track, with module/value/error provenance distinct from erl_eval; retain all 249 expression cases. Continue audited lists/OTP adapter contracts and signal race hardening. Current registry 56, lists 13/91 (+3 versus part-028), local/CI tests 344; broad assignment remains approximately 5%.

## Part 031 checkpoint

Publish the locally validated compiled-module track and dispatch the existing exact-version workflow against that head. Verify **249/249 expressions** and **14/14 modules** separately (**263 aggregate**), preserve same-head CI 361 tests and source/hash provenance; do not infer module results from historical 249-case reports. Next expand compiled fixtures to error/evaluation ordering and dynamic bit sizes, then grammar/OTP callbacks/signals. Registry 56 and lists 13/91 are unchanged; broad assignment remains approximately 5%.

## Part 032 checkpoint

Both tracks and same-head Windows CI are complete: 249 expressions,14 compiled modules,361 tests. Next expand compiled fixtures for dynamic bit-size evaluation/error ordering and closure/scope edges, then broader grammar/OTP callback adapters and deterministic signal races. Retain both existing corpora and their separate modes/hashes. No new MFA in this increment; registry 56, lists 13/91 and overall approximately 5% remain unchanged. Full optimizer/debug/resources/reference suites still require separate work.

## Part 033 checkpoint

Publish the completed atom/constants/style increment after full validation. Current local tests369, registeredMFAs56, lists13/91; approximately5% overall. The fixed common atom cache is implemented; arbitrary atom/resource-limit semantics remain separate unfinished work. Continue part-032 compiled fixture expansion for bit-size evaluation/error ordering and closure scopes, then grammar/OTP callbacks/signals. Retain all249 expression and14 compiled fixtures and their independent expected values. Remote oracle263/263 at b13b56a remains historical evidence, not a rerun for this allocation refactor.
