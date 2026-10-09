# Next executable tasks

Read PROGRESS and semantic-differences first; preserve existing tests/sample/package integration.

1. **Extend exact-version oracle comparison.** Current run 37955525454 at 666c4b5 matched **160/160** expressions against OTP-29.1.1; original report/verified metadata are in validation/part-017. Local executable remains absent. Keep this corpus, add cases with each component, dispatch/inspect each tested head and preserve actual reports. Current reference evaluation uses erl_eval:exprs. Add a separate compiled-reference-module track before asserting compiler/optimizer equivalence; stacks, asynchronous signals and side effects also need separate tracks. Do not change baseline or introduce production BEAM.
2. **Expand bit syntax and map scopes.** Selected integer/binary/float/UTF construction/patterns, size/map BIFs, fun capture and numeric coercion cases match the current oracle. Implement construction-only integer/float literal-string modifiers, including empty-string error validation. Pinned erl_lint good_string_size_type permits only default or unsized UTF strings in patterns; preserve those diagnostics. Continue scopes/map error precedence and full .erl/hybrid/PackageReference validation. Full MFA inputs/stacks/side effects/reference suites remain partial.
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
