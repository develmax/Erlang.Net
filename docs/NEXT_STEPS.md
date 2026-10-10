# Next executable tasks

Read PROGRESS and semantic-differences first; preserve existing tests/sample/package integration.

1. **Extend exact-version oracle comparison.** Current run 38024604952 at b7b8c72 matched **419/419 expressions +48/48 compiled modules +17/17 diagnostics (484/484)** against OTP-29.1.1; original reports/metadata are in validation/part-047. Initial abort/fix is preserved in part-025; earlier 160-case success remains historical in part-017. Local executable remains absent. Keep this corpus, add cases with each component, dispatch/inspect each tested head and preserve actual reports. Reference tracks now separately use erl_eval:exprs and compile:forms/code:load_binary versus generated C# assemblies. Expand selected compiled cases before broader compiler/optimizer claims; stacks, asynchronous signals and side effects also need separate tracks. Do not change baseline or introduce production BEAM.
2. **Extend grammar and bit/map scopes.** Begin and ordinary operators are implemented; continue remaining construct grammar/OTP callbacks, preserving484 reference cases and explicit stack/resource limitations. Selected integer/binary/float/UTF construction/patterns, size/map BIFs, fun capture and numeric coercion cases match the current oracle. Integer/float literal-string construction is now implemented with empty-string validation; the complete 249-case corpus now passes; harden construction evaluation/error ordering and resource boundaries next. Pinned erl_lint good_string_size_type permits only default or unsized UTF strings in patterns; preserve those diagnostics. Continue scopes/map error precedence and full .erl/hybrid/PackageReference validation. Full MFA inputs/stacks/side effects/reference suites remain partial.
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

## Part 034 checkpoint

Publish twelve compiled bit-order/closure fixture additions and dispatch the pinned workflow on the exact implementation head. Require separate counts249 expressions/26 modules (275 total), completeness/version verification, no aborts, independent expected values and all old/new source/generated hashes; inspect same-head Windows CI381/full integration. Preserve actual artifacts as the next evidence part before promoting the selected new cases. Next substantive feature work: audited library contracts or missing grammar/OTP callbacks/signals; full compiler/atom resources/stack/debug scope remains unfinished.

## Part 035 checkpoint

Publish the locally validated construction-scope fix and compiler-rejection track, then rerun the existing pinned workflow on the corrected head. Require complete249 expressions/28 compiled modules/5 diagnostic cases (282 total), no abort/mismatch, same-head389-test CI and source/generated/diagnostic hash provenance. Keep the initial268-case abort immutable; historical263/263 is not corrected-head proof. After verification, implement audited lists:last/1 and lists:split/2, distinguishing empty/nonlist/improper-tail/zero/exact/overrun/large-index boundaries; then grammar/OTP callbacks/signals. Registry56/lists13/91/approximately5% overall.

## Part 036 checkpoint

Corrected three-track reference and same-head CI are complete and preserved:249 expressions,28 modules,5 diagnostics,282 total,389 tests. Next implement audited lists:last/1 and lists:split/2, with direct MFA cases plus proper/improper/empty/nonlist/zero/exact/overrun/large-index reference boundaries; keep the full282-case baseline. Further grammar/OTP callbacks/signals, full construction/scoping/diagnostic locations/warnings/debug/resources/reference suites remain unfinished. Registry 56/lists 13/91/approximately 5% overall.

## Part 037 checkpoint

Publish the completed last/1 and split/2 increment, dispatch pinned OTP on the exact head and preserve same-head CI plus complete **271 expressions /28 modules /5 diagnostics (304 total)**, all source/hash provenance and zero aborts. Local414 tests/full pipeline pass; registry58/lists15/91. Then implement audited lists:member/2 exact equality and lists:append/1 tail/error contracts with full reference boundaries, followed by missing grammar and Erlang OTP callback adapters/signals. Approximately5% overall; full assignment remains incomplete.

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

## Part 043 handoff

User-requested remaining semantic strings are extracted with319 value-preserving references, full472-test integration and149-type layout audit. Current registry62/delta0; lists19/91. Preserve immutable part043 local audits and historical part042355/355 separately. Continue audited compiler if expressions and branch bindings; source templates/test fixtures keep independent literal data. Keep domain constants separate even when their values coincide.

## Part 044 handoff

Publish tested if implementation, dispatch exact pinned392-case oracle (346 expressions/36 modules/10 diagnostics) and inspect same-head516 CI; preserve source/hash/expected/class/reason and artifact/head/reference-build provenance. Existing355/355 proof is historical. After verification, extend remaining grammar (begin blocks and additional unary/boolean/bit operators) with pinned error/binding contracts, then Erlang OTP callback adapters/signals. Registry62/delta0; lists19/91; approximately5% overall.

## Part 045 handoff

Published if head c7e575b passes exact392/392 and same-head516 CI. Preserve immutable source/hash/expected/diagnostic/head/job/archive metadata; registry62/delta0, lists19/91, approximately5%. Next implement audited begin blocks with binding/export/order errors and missing operators across interpreted/generated/hybrid paths; then Erlang OTP callback adapters/signals. No full grammar/module/resources/annotations/reference-suite claim.

## Part 046 handoff

Publish begin/operators implementation, dispatch exact pinned484 cases (419 expressions,48 generated/compiled modules,17 diagnostics), inspect same-head614 CI and preserve source/hash/expected/class/reason/provenance. Historical392/392 remains part045. Next Erlang OTP callback adapters or remaining construct-specific grammar; full records/comprehensions/maybe/try, annotations/stacks/resources/signals/reference suites incomplete. Registry62/delta0, lists19/91, approximately5% overall. Near-bound enormous allocations are local-only and must not enter reference corpus.

## Part 047 handoff

Begin/ordinary operators head b7b8c72 passes pinned484/484 (419 expressions,48 modules,17 diagnostics), same-head614/full CI. Preserve immutable provenance; registry62/delta0, lists19/91, approximately5% overall. Next remaining grammar (try/records/comprehensions/maybe) or Erlang OTP callback adapters/signals; complete source spans/stacks/resource contracts and reference suites remain incomplete. Do not add near-bound huge positive allocation cases to the oracle.

## Part 048 handoff

Publish local648/full-pipeline source-directed expression binding port; dispatch pinned518 cases (439 expressions,54 compiled modules,25 diagnostics), inspect same-head648 CI and preserve strict historical/new expected/hash/code/message evidence. Then port eval_map_fields/map base scopes and eval_bits callbacks against exact source. Keep current Pratt/name-only lint/trace/resource departures explicit in OTP-PORTS.md. Full assignment remains approximately5%; registry62/delta0.

## Part 049 — Compiled binding mismatch preserved

Same-head648/full CI at9ae76d7 passes, but complete/version-verified pinned518 comparison returns517 passed/one failure:439 expressions,53/54 compiled modules,25 diagnostics. Compiled tuple conflict returns badmatch2 in OTP; shared evaluator/fixture returns badmatch1, which is correct for erl_eval. Preserve [raw evidence](validation/part-049/checkpoint.json); no518/518 claim. Next separate compiled policy with source-guided nonconstant/order/closure checks. Dated map/bit audit correction is in OTP-PORTS.md; runtime sequential evaluation must not be confused with independent static scope checks. Registry62/delta0, approximately5%; full lowering/grammar/traces/resources incomplete.

## Part 050 — Compiled binding correction

Separate ModuleDefinition binding subset policy corrects the preserved compiled badmatch2 versus interpreter badmatch1 mismatch. Independent child/function capture scopes retained; four additional compiled nonconstant/call/cons-order/function-local cases pass. The single old compiled FixtureExpected correction follows official retained part049 output and keeps source/generated hashes. Local **652/652/full integration** passes; [proof](validation/part-050/checkpoint.json). Corpus439/58/25=522 awaits new-head reference/CI after publication; do not claim517/518 failure resolved against OTP until compared. Historical484/484 remains separate.

The adapter is not full Core lowering: internal child-body constraint timing, known-variable states, IR/optimizer/source stacks/resources remain incomplete. Current native hybrid standalone lowering retains interpreter semantics.159 types/no violations; registry62/delta0, approximately5% overall. Source notices/full license/updated register remain in local packages. Next verify522 then targeted map base/static scopes and deeper Core lowering.

## Part 051 — Verified distinct compiled/interpreted binding paths

Implementation head **2bf65a001186e19555b21ca1f44985e4ab830bf3** passes **439/439 expressions +58/58 compiled modules +25/25 diagnostics =522/522**, complete/version verified against source-built OTP-29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5. Same-head Windows CI passes **652/652/full integration**. The original compiled tuple conflict now matches official badmatch2 while erl_eval remains badmatch1; added nonconstant/call/cons-side-effect/function-local checks pass. [Raw proof](validation/part-051/remote-checkpoint.json), [strict provenance](validation/part-051/corpus-audit.json). Historical439 expressions/25 diagnostics and54 module sources/generated hashes remain unchanged. The sole prior FixtureExpected correction matches the retained part049 official reference outcome; its517/518 failure is preserved.

159 namespace-level types/no violations, registry62/delta0 against immutable part050 tests. erlang40/351,lists19/91,maps2/34,io1/53 are declaration presence, not semantic completion. Approximately5% coarse overall estimate. Local full pipeline stays part050 evidence; no executable change or rerun in this evidence part. Source-directed port register/notices remain separate from a claim of full compiler port.

Full Core known-variable/IR/internal child-body constraint timing/optimizer/stacks/resources/grammar/lint remain incomplete. Native hybrid standalone expressions retain the interpreter route. Next targeted map base/static scopes and deeper Core lowering; sequential map/bit runtime order must remain distinct from static isolation. No whole-module promotion/new dependency/production BEAM/public NuGet or original-code public license selection.

## Part 052 — Map scopes and indexed exact-key lookup

Local **676/676/full integration** passes; [reports](validation/part-052/checkpoint.json). Map base and fields have independent interpreter input scopes; fields remain sequential, type/key checks precede the final merge. Static base/key/value scopes are independent; compiled modules use the distinct Core subset adapter. New corpus **449 expressions/63 modules/31 diagnostics=543** awaits new-head oracle/CI. Historical522/522 remains part051 at2bf65a0. Prior439/58/25 sources/outcomes/generated hashes unchanged;62 MFAs/delta0,161 namespace types/no violations; approximately5% coarse overall estimate.

MapTerm retains its privately owned exact-key dictionary for Get/TryGet, preserving sorted read-only entries, distinct numeric/signed-zero keys and input immutability. [Release microbenchmark](validation/part-052/map-lookup-benchmark.json) compares the old C# scan with current indexed lookup on identical integer-key queries. Construction/retained memory/BEAM/end-to-end performance are outside this measurement. Full Core/lint/grammar/resources/stacks remain incomplete. Use C# optimizations that preserve observable contracts; cross-runtime superiority requires matched benchmarks. Next verify543/same-head676 CI, then deeper Core match timing and deferred binary errors.

## Part 053 — Verified map scopes and exact-key index

Implementation head **0fe8a7265f827e7c5e9e5010b762ceb2d75162e2** passes **449/449 expressions +63/63 compiled modules +31/31 compiler diagnostics =543/543** against exact source-built OTP-29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5. Same-head Windows CI passes **676/676/full integration**. [Raw provenance](validation/part-053/remote-checkpoint.json), [strict contract/hash audit](validation/part-053/corpus-audit.json). All prior 439/58/25 expression/module/diagnostic contracts and generated hashes remain unchanged; ten independent map expectations, five compiled outcomes and six rejections match. Distinct interpreted base/field conflict and compiled conflict outcomes are verified for these inputs.

161 namespace-level types/no violations,62 MFAs / delta 0 against immutable part052 tests. API declarations erlang40/351,lists19/91,maps2/34,io1/53 are presence, not semantic completion; approximately 5% coarse overall estimate. Local full pipeline and old/new C# lookup measurements remain part052 evidence; no executable change or benchmark rerun here. [Performance policy](PERFORMANCE.md) records the measured workload, retained-memory tradeoff and requirements for a BEAM comparison. No cross-runtime speed claim.

Full Core lowering/internal child-body constraints/optimizer, complete lint/grammar/stacks/resources/runtime/OTP/reference suites remain unfinished. Native hybrid standalone expressions retain interpreter semantics. Next deeper Core match timing and deferred binary error ordering, then missing grammar/Erlang OTP callback adapters. Preserve failed historical evidence; no whole-module promotion/dependency/production BEAM/public NuGet/license change.
