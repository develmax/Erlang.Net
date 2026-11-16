# Part 1506 — Keep call results in registers for arithmetic selectors

The entry planner recognizes a `case` selector whose arithmetic combines one ordinary call with a literal or an entry argument. It excludes the call arguments from the post-call reference scan, retains the other operand and values needed by the continuation, and feeds the call result from X0 to the arithmetic instruction. The existing case emitter consumes its result in X0. The same plan handles a terminal case and a case whose result is bound before further expressions. [Entry planning](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.GeneralEmitter.cs) · [Arithmetic selector](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.Subtraction.cs).

The reference is the pinned OTP `beam_ssa_pre_codegen` frame and live-interval passes and the release `xmerl_xsd_type:pow/2` disassembly. The compiler reuses its entry call, reference analysis, and case emitters. No dependency was added.

The fixture checks both operand orders and a following expression. Its frame sizes move from three to one, three to two, and four to three, with OTP using one slot in each. The three arithmetic operations take the call result directly from X0. OTP and both C# images agree on 192 returns, errors, and module frames in exception stacks. [Fixture](beam_case_call_arithmetic.erl) · [Oracle](case-arithmetic-probe.escript) · [Results](fixture.txt).

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1506-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1506-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1506-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1506-final.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
dotnet artifacts/beam-backend/part1506-tests/Erlang.Tests.dll --case compiler/beam/documentation-payload-matches-otp --case compiler/beam/rebuild-installed-otp-release-images --case compiler/beam/whole-module-matches-otp --case compiler/beam/call-result-bif-keeps-register --case compiler/beam/call-result-case-keeps-register --report artifacts/beam-backend/part1506-focused.json
dotnet artifacts/beam-backend/part1506-tests/Erlang.Tests.dll --block compiler --report artifacts/beam-backend/part1506-compiler.json
```

Both builds pass with no warnings or errors. All five focused .NET tests pass. The corpus, harness, and fixture tools use the same compiler hash. [Focused tests](focused.json) · [Compiler hashes](compiler-hash.txt).

The complete Windows OTP 29.1.1 run accepts all 1,366 sources and generates all 1,366 images. There are 385 Equal and 981 Different, with no status transitions. `ELDAPv3` remains Equal after C# ASN.1 generation. OTP loads both images in all 981 differing pairs. [Corpus](otp-corpus.json) · [Loader](loader-and-code.txt).

`asn1ct_imm:debit/2` and `xmerl_xsd_type:pow/2` move from three slots to two against OTP's one. Their modules' combined Code size gap falls by 18 bytes. Neither body reaches normalized equality. `pow/2` still uses generic multiplication instead of the float instruction sequence and retains a branch spill. In 225 calls through `compare_floats/2` the old and new images agree, including 18 pre-existing exception-stack differences from OTP. [Frames](frame-delta.txt) · [Instructions](xmerl-pow.txt) · [Release probe](xmerl.txt).

Among functions with an oversized entry frame and straight-line preparation for the first ordinary call, 1,318 functions in 449 modules still save an incoming register to a Y slot that does not appear after the call. The largest group with source information is a tuple pattern matched against the first local call result, followed by a continuation. It covers 285 functions in 128 modules. The corresponding remote-call group covers 65 functions in 55 modules. These are overlapping candidate groups, and the count does not predict whole-file matches. [Before](first-call-spills-before.txt) · [After](first-call-spills-after.txt) · [Tuple continuation instructions](xmerl-duration-atoms.txt).

The compiler block passes 3,721 of 3,829 tests, with the same 108 failures as Part 1505. The scoped layout check reports existing formatting differences in the two edited source files. The moved pattern's indentation was aligned with the formatter after testing. The full .NET suite was not rerun. Canonical readiness and publication remain pending. The readiness delta retains the immutable Part 561 baseline. [Compiler block](compiler-tests.json) · [Test status comparison](compiler-test-status.txt).
