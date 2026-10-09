# Next executable tasks

Read PROGRESS and semantic-differences first; preserve existing tests/sample/package integration.

1. **Extend exact-version oracle comparison.** The local executable remains absent. Remote run 37943457369 at c139b45 matched all 93 value/error cases against pinned OTP-29.1.1; original report is under validation/part-012. The current 154-case corpus includes 30 float and 31 UTF cases; float run 37952290309 at 19774f9 matched 123/123; dispatch/inspect the UTF head. Add new segment/scope/error cases as implementation expands, dispatch the existing workflow on each tested head, preserve actual reports and fix mismatches. Stacks, asynchronous signals and side effects need separate comparison tracks. Do not alter the baseline or add a production Erlang dependency.
2. **Expand bit syntax and map scopes.** Existing integer/binary construction/patterns, size BIFs and clause-local fun captures have a successful 93-case oracle corpus; full MFA/stacktrace/side-effect coverage remains pending. Float segments (16/32/64, endianness, finite matching, nearest-even integer conversion) now pass local validation. UTF8/16/32 and UTF string modifiers now pass 293 local tests, including malformed/unaligned inputs and generated integration. Resolve the new UTF oracle cases, then add integer/string size modifiers and broader scope/error contracts, with pinned source contracts and full .erl/hybrid/PackageReference validation. Continue pattern-scope combinations and map error precedence.
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
