# Part 1502 — Infer integer ranges for internal parser selectors

The compiler propagates integer intervals and integer-list element intervals through local call arguments. It joins facts at call entries and follows literals, variable bindings, list construction, head and tail, list patterns, and branches. Exported and native entries, on-load functions, and named function references are analyzed with unknown arguments. An integer alias pattern supplies the matched singleton to the bound variable. The existing register tail-dispatch emitter writes the inferred bounds into the typed selector. Pinned OTP `beam_ssa_type` and `beam_ssa_codegen` supply the reference. The analysis reuses source AST traversal, BIF resolution, and compact type emission. No dependency was added. [Analysis](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.EntryIntegerRanges.cs) · [Dispatch](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.GeneralEmitter.cs#L836).

In the control fixture, OTP and the current compiler attach the interval `0..4` after the state crosses a list and two local calls. The exported selector and a function reachable via a named function reference remain untyped, including the latter's known direct callers. The OTP VM observes the same values and errors on all 96 inputs. In `core_parse`, the first preview conservatively inferred `0..512`. Narrowing alias patterns to their matched integer gives the OTP interval `0..511`. [Fixture](beam_entry_range.erl) · [Execution and operands](fixture.txt) · [Core parser](core-parse.txt).

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1502-preview2-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1502-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1502-preview2-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1502.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
dotnet artifacts/beam-backend/part1502-tests/Erlang.Tests.dll --case compiler/beam/documentation-payload-matches-otp --case compiler/beam/rebuild-installed-otp-release-images --case compiler/beam/whole-module-matches-otp --case compiler/beam/call-result-bif-keeps-register --case compiler/beam/call-result-case-keeps-register --report artifacts/beam-backend/part1502-focused.json
dotnet artifacts/beam-backend/part1502-tests/Erlang.Tests.dll --block compiler --report artifacts/beam-backend/part1502-compiler.json
```

Five focused checks pass with the same compiler binary used for the corpus. [Focused results](focused.json) · [Compiler hashes](compiler-hash.txt).

The complete Windows OTP 29.1.1 comparison admits all 1,366 sources and produces 1,366 images. It records 385 Equal and 981 Different, including Equal for the C# ASN.1 generated `ELDAPv3`. OTP loads both images in all 981 differing pairs. [Corpus](otp-corpus.json) · [Transitions](status-changes.txt) · [Loader](loader-and-code.txt).

Fourteen images change. All 253 new typed goto selectors use the same bounds as OTP. Their functions gain normalized instruction equality. The reachable graphs agree for all 6,290 functions in the changed modules after label, jump, and type normalization. The Code size gap grows by 506 bytes from the 253 typed wrappers, while each of the 253 affected function bodies now matches after normalization. [Ranges](parser-ranges.txt) · [Normalized functions](normalized-functions.txt) · [Graph comparison](reachable-graph.txt) · [Code and frame totals](summary.txt).

OTP and both C# images agree on 11 Core parser results and 323 Erlang parser results and errors. The probes are finite samples. The next largest missing numeric annotation group is `gc_bif '+'`, with 182 occurrences across 26 modules. [Core parser](core-parse.txt) · [Erlang parser](erl-parse.txt) · [Type ranking](rank-types.txt).


The compiler block passes 3,721 of 3,829 tests. The same 108 tests fail in the Part 1501 baseline. The full .NET suite was not rerun. Canonical readiness and publication remain pending. The readiness delta uses the immutable Part 561 baseline. [Compiler block](compiler-tests.json) · [Test status comparison](compiler-test-status.txt).

The [execution probe](range-probe.escript) accepts `fixture`, the fixture source, and two BEAM paths, or `core_parse` or `erl_parse` followed by the pinned OTP install and the two generated-image directories. The [range inventory](parser-range-inventory.escript) compares the inferred bounds at emitted goto selectors. The [numeric control](numeric-controls.txt) shows that OTP leaves an unproven addition operand untyped and adds numeric information after a guard or successful arithmetic result.
