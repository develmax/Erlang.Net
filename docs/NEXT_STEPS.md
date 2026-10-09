# Next executable tasks

Read PROGRESS and semantic-differences first; preserve existing tests/sample/package integration.

1. **Obtain a development-only OTP 29.1.1 oracle.** The local source tag is pinned; erl/erlc and Docker were not found, and WSL listing did not provide a usable distro. Prefer an isolated portable runtime under artifacts or the manual `.github/workflows/differential.yml` definition at the exact baseline; this workflow has not run. Do not alter the baseline or add a production Erlang dependency. Run `dotnet run --project tools/Erlang.Differential -- <path-to-erl> artifacts/differential.json`; it checks releases/29/OTP_VERSION before comparing ETF results. Extend numeric/maps/signed-zero/error/guard and signal cases, then fix any discrepancies. Record actual results separately from source-derived tests.
2. **Harden map contracts, then add binary source syntax.** Part 004 implements map construction/updates/patterns with exact keys, guard-expression keys, transactional bindings and generated .erl/hybrid integration. Part 005 adds is_map/map_size/map_get/is_map_key as guard BIFs with exact keys and badmap/badkey regressions. The differential corpus contains 37 value/error cases; actual oracle results must be recorded separately. Obtain oracle evidence and audit key-scope/error edge cases. Next language work: bit-syntax AST, size/unit/type rules and binary patterns against pinned reference sources.
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
