# Next executable tasks

Read PROGRESS and semantic-differences first; preserve existing tests/sample/package integration.

1. **Complete exact-version oracle comparison.** The local executable remains absent. Use the manual pinned .github/workflows/differential.yml or an isolated development runtime. Run 37941798273 at 8d35ea8 built OTP-29.1.1 and matched 52/90 cases, then aborted on invalid source <<1:-1>>. Part 011 enforces the pinned primary-size grammar and corrects it to <<1:(-1)>>. The current corpus has 93 cases, including bit patterns, bit size BIFs and clause-local fun captures not yet verified. Dispatch on the current head, inspect/save the actual report, fix mismatches and preserve partial evidence. Do not alter the baseline or add a production Erlang dependency.
2. **Harden bit syntax and map scopes.** Map construction/updates/patterns and map guard BIFs have limited real oracle evidence in the 52 matched cases; full MFA/stacktrace/side-effect coverage remains pending. Integer/binary construction exists. Part 009 adds signed/unsigned big/little/native patterns, guard sizes, prior-segment bindings, binary tails and captured size shadowing; part 010 fixes per-clause fun capture. Verify these against the oracle, add full pattern-scope combinations, then float/UTF segments and string modifiers. Preserve generated .erl/hybrid/PackageReference integration and exact diagnostic boundaries.
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


Part 010: inspect run 37941798273 at 8d35ea8 (90 cases), fix any discrepancies, then run the current 93 cases including clause-local fun capture regressions. Continue full bit-pattern scope combinations and remaining segment types.
