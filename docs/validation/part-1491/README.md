# Part 1491 — Keep case branch fields in X before a continuation

The saved-selector tuple arm planner now also handles cases with later expressions when there are no joined bindings and the branch yields its result in X0. The branch returns control to the existing join and preserves the selector in Y. The tail-call condition explicitly excludes a pending continuation. The pinned `beam_ssa_pre_codegen` frame and liveness passes and the OTP disassembly provide the reference. [Compiler](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.GeneralEmitter.cs#L10424).

A source scan of the previous 981 differing images finds 82 functions in 67 modules with an assignment from a variable-selector case followed by later expressions and a tagged arm that uses a field. This scan identifies candidates. The binary comparison measures actual changes. [Source scan](source-continuations.txt).

The permanent `saved_tuple_case/2` control now projects payloads into X in the eligible arms. It uses three frame slots, down from four. OTP uses one, checks the type in X, and builds the outer result inside each arm. OTP and both C# builds agree on the tested return values, guard fallback, and the original unmatched tuple in `case_clause`. [Fixture](../../../tests/Erlang.Tests/Resources/Beam/beam_entry_result_case.erl#L120) · [Disassembly and execution](probe.txt).

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1491-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1491-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1491-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1491.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
dotnet artifacts/beam-backend/part1491-tests/Erlang.Tests.dll --case compiler/beam/documentation-payload-matches-otp --case compiler/beam/rebuild-installed-otp-release-images --case compiler/beam/whole-module-matches-otp --case compiler/beam/call-result-bif-keeps-register --case compiler/beam/call-result-case-keeps-register --report artifacts/beam-backend/part1491-focused.json
```

All 1,366 Windows OTP 29.1.1 sources are admitted and generate a BEAM. The byte comparison records 385 Equal and 981 Different. `ELDAPv3` remains Equal after C# ASN.1 generation. OTP accepts both images in all 981 differing pairs. [Corpus](otp-corpus.json) · [Status transitions](status-changes.txt) · [Loader](loader-and-code.txt).

Seventy-one release images change, with 89 changed function bodies. The measured frame moves closer to OTP in 40 of them. The absolute Code size gap drops by 343 bytes. No function gains or loses normalized instruction equality. The largest source group among oversized nonzero frames is a single unguarded clause with a case body, at 1,720 functions in 483 modules. A remote call as the selector accounts for 738 functions in 342 modules. [Frame totals](summary.txt) · [Normalized comparison](normalized-functions.txt) · [Source ranking](frame-source-after.txt) · [Selector ranking](frame-case-after.txt).

Five focused checks pass with the same compiler binary as the corpus tool. The broader `--block compiler` run is pending at this checkpoint. The full .NET suite was not rerun. Canonical readiness and publication remain pending. The readiness delta uses the immutable Part 561 baseline. [Focused checks](focused.json) · [Compiler hashes](compiler-hash.txt).
