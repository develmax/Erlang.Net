# Part 1505 — Propagate numeric sign bounds into octet loops

The compiler refines numeric facts on both outcomes of a comparison with an integer literal. It uses Erlang term order to prove numeric input on the branch below a number. A lower numeric bound requires independent evidence that the input is numeric. The joined call analysis carries the bound through forwarding functions and the recursive bit shift. A failed function-clause guard refines the next clause only when the preceding head can match every argument. [Numeric flow](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.NumericPairFlow.cs).

The octet emitter uses the bound on the exact comparison and the shift. After the shift succeeds, the mask uses the corresponding integer bound. Existing integer range proofs take precedence. The reference is pinned OTP `beam_ssa_type` and the optimized SSA and release disassembly for the octet helpers. No dependency was added. [Emitter](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.LengthEncoding.cs).

The fixture matches OTP's nonpositive and nonnegative numeric operands before the shift and integer operands after it. The unsigned fixture passes through a private function with an ordered guard. The unguarded control retains raw comparison and shift operands and still lacks the unbounded integer operand on its mask. OTP and both C# images agree on 46 returns and exceptions. The Megaco control agrees on 36 observations and preserves the addition annotations from Part 1504. [Fixture source](beam_octet_ranges.erl) · [Probe](octet-probe.escript) · [Fixture results](fixture.txt) · [Megaco results](megaco.txt).

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1505-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1505-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1505-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1505-final.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
dotnet artifacts/beam-backend/part1505-tests/Erlang.Tests.dll --case compiler/beam/documentation-payload-matches-otp --case compiler/beam/rebuild-installed-otp-release-images --case compiler/beam/whole-module-matches-otp --case compiler/beam/call-result-bif-keeps-register --case compiler/beam/call-result-case-keeps-register --report artifacts/beam-backend/part1505-focused.json
dotnet artifacts/beam-backend/part1505-tests/Erlang.Tests.dll --block compiler --report artifacts/beam-backend/part1505-compiler.json
```

Both builds pass with no warnings or errors. Five focused tests pass. The compiler hashes agree across the corpus, test harness, and fixture tools. [Focused tests](focused.json) · [Compiler hashes](compiler-hash.txt).

The complete Windows OTP 29.1.1 corpus admits all 1,366 sources and generates all 1,366 images. There are 385 Equal and 981 Different, with no status transitions. `ELDAPv3` remains Equal after C# ASN.1 generation. OTP loads both images in all 981 differing pairs. [Corpus](otp-corpus.json) · [Transitions](status-changes.txt) · [Loader](loader-and-code.txt).

All 29 changed function bodies across 23 images gain normalized equality. The 33 missing numeric exact-comparison operands fall to four, and the 55 missing integer mask operands fall to 26. The current and previous C# graphs agree for all 8,462 functions after label, jump, and type normalization. The additional type operands increase the absolute Code size gap by 212 bytes. [Function delta](normalized-functions.txt) · [Type ranking before](rank-types-before.txt) · [Type ranking after](rank-types.txt) · [Graphs](reachable-graph.txt) · [Sizes](summary.txt).

Both changed `ECPrivateKey` helpers have OTP's operand types. The reference and both C# images agree on 72 encode observations, including negative integers, invalid versions, and lengths that require more than one octet. [Release probe results](ec-private-key.txt).

The largest first-difference group is an oversized `allocate` with the same live-register operand. It occurs in 11,237 functions across 885 modules. In `xmerl_xsd_type:pow/2`, C# saves the exponent even though it is consumed by the first call and spills the call result before using it. OTP preserves only the mantissa across that call. [Frame ranking](frame-ranking.txt) · [Instructions](xmerl-pow.txt).

The compiler block passes 3,721 of 3,829 tests, with the same 108 failures as Part 1504. The full .NET suite was not rerun. Canonical readiness requires a full passing report and remains pending, as does publication. The readiness delta retains the immutable Part 561 baseline. [Compiler block](compiler-tests.json) · [Test status comparison](compiler-test-status.txt).
