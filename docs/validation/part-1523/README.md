# Part 1523 — Trim stack slots before calls in tuple case arms

For a terminal tuple case with an atom alternative, a successful arm can reuse a stack slot that is dead after its pattern checks and unused by its next call. The generator keeps the original selector for case_clause, loads the continuation arguments, moves the payload into the retained slot, and trims the frame to one slot. The alternative still has access to its original inputs. The continuation builds its result tuple from the payload and the call result in X0.

The change reuses the existing selector, tuple projection, input type proof, and tuple result emitters. The reference is OTP 29.1.1 at ad05823719d77c8faee87348ea39513d4e2f99c5. No dependency was added. [Compiler implementation](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.TupleCaseCallContinuation.cs).

The deterministic fixture matches OTP with SHA-256 74F2754E422685C68F0AF71BB0E8E94750EA23B20C48F13DD182F79122E4A305. The oracle compares 80 values, exceptions, and fixture stack observations, including exact integer and float key distinctions and rejected tuple shapes. [Fixture source](../../../tests/Erlang.Tests/Resources/Beam/beam_tuple_case_continuation.erl) · [OTP fixture](../../../tests/Erlang.Tests/Resources/Beam/beam_tuple_case_continuation.beam) · [Oracle result](fixture.txt).

## Commands

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1523-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1523-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1523-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1523-final.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
$tests = 'artifacts/beam-backend/part1523-tests/Erlang.Tests.dll'
dotnet $tests --case compiler/beam/atom-case-emits-local-tails --case compiler/beam/documentation-payload-matches-otp --case compiler/beam/rebuild-installed-otp-release-images --case compiler/beam/whole-module-matches-otp --case compiler/beam/call-result-bif-keeps-register --case compiler/beam/call-result-case-keeps-register --report artifacts/beam-backend/part1523-focused.json
```

## Results

All 1,366 release sources were accepted and generated. The comparison found 387 Equal and 979 Different. ELDAPv3 still matches from the C# ASN.1 generator. The two changed release images are xmerl_sax_simple_dom and xmerl_sax_old_dom. Their find_and_remove_attribute/3 functions now match normalized OTP instructions, including the three-slot entry frame and trim to one slot. Each Code chunk grows by four bytes. Other functions in both modules still differ. OTP loads all 979 differing images.

[Full corpus](otp-corpus.json) · [Function delta](normalized-functions.txt) · [Frame delta](frame-delta.txt) · [Loader and Code](loader-and-code.txt) · [Simple DOM instructions](xmerl_sax_simple_dom.txt) · [Old DOM instructions](xmerl_sax_old_dom.txt) · [Focused tests](focused.json).

The source audit finds 23 functions in 23 images where an unguarded terminal tuple arm binds a simple call result and continues through multiple expressions. The corresponding simple tuple-return group fell from three candidates to one. [Before](continuation-shapes-before.txt) · [After](continuation-shapes-after.txt) · [Scan script](tuple-case-continuation-shapes.escript).
The compiler block passed 3,722 of 3,830 tests. The same 108 tests failed in Part 1522. The full .NET suite and canonical readiness were not rerun. [Compiler block](compiler-test-status.txt).
