# Part 1489 — Dispatch saved case selectors by tuple arity and tag

The case emitter reuses the arity and tag planner for a selector held in a Y register. The arity table reads the saved selector directly, and the tag extractor reads the same tuple. The field matcher reuses the arity proof, checks a remaining tag in X, and skips the tag comparison when the value table has checked it. The original selector remains available for later clauses and `case_clause`. The pinned OTP `beam_core_to_ssa` source and release disassembly provide the reference. [Case emitter](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.GeneralEmitter.cs) · [Baseline source candidates](../part-1488/arity-case-after.txt).

The `saved_tuple_case/2` fixture makes a call before dispatch and returns the original selector alongside the selected result. It exercises different arities, a failed guard followed by a repeated tag, and `case_clause` with the original unmatched tuple. Both C# builds and OTP agree on the tested values and error. The final C# candidate dispatches directly from Y as OTP does. OTP also projects the payload in X and constructs the returned tuple inside the selected arm. [Fixture](../../../tests/Erlang.Tests/Resources/Beam/beam_entry_result_case.erl) · [Instruction and result comparison](probe.txt).

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1489-checked-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1489-checked-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1489-checked-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1489-checked.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
dotnet artifacts/beam-backend/part1489-checked-tests/Erlang.Tests.dll --case compiler/beam/documentation-payload-matches-otp --case compiler/beam/rebuild-installed-otp-release-images --case compiler/beam/whole-module-matches-otp --case compiler/beam/call-result-bif-keeps-register --case compiler/beam/call-result-case-keeps-register --report artifacts/beam-backend/part1489-checked-focused.json
```

All 1,366 Windows OTP 29.1.1 sources are admitted and generate a BEAM. The comparison records 385 Equal and 981 Different. `ELDAPv3` remains Equal after C# ASN.1 generation. OTP accepts both images in each differing pair. [Corpus](otp-corpus.json) · [Status changes](status-changes.txt) · [Loader](loader-and-code.txt).

The change affects 346 release images and 1,369 instruction bodies. Another 336 arity tables and 187 tuple value selectors match the OTP arity or key sets. An intermediate run found 194 frame sizes that moved farther from OTP. The final X scratch checks remove that increase and bring 191 frame sizes closer. The aggregate absolute Code size gap drops by 4,023 bytes. Two `dialyzer_utils` failure stubs exchange their source line annotations when the case branches change emission order. [Arity selectors](new-arity-selectors.txt) · [Value selectors](new-value-selectors.txt) · [Frame totals](summary.txt) · [Preliminary frame increases](preliminary-frame-increase.csv) · [Failure stub lines](dialyzer-inline-lines.txt).

Five focused checks pass with the same compiler binary as the corpus tool. The full .NET suite was not run on this build. Canonical readiness and publication remain pending. The readiness comparison baseline remains the immutable Part 561 snapshot. [Focused checks](focused.json) · [Compiler hashes](compiler-hash.txt).
