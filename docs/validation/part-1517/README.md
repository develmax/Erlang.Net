# Part 1517 — Keep tuple call results in registers across entry branches

The general entry branch emitter uses the existing register tuple-match path when a direct call supplies a tuple of variable fields followed by a direct tail call with simple operands. The tuple result stays in X0 while the emitter checks its shape and projects the live fields into outgoing argument registers. A frame retains entry arguments only when the continuation or a match needs them. Failed matches branch to handlers emitted after the normal clause bodies. An external tail immediately after an optimized branch receives the preceding emitted source location for line deduplication.

Pinned OTP 29.1.1, commit ad05823719d77c8faee87348ea39513d4e2f99c5, classifies statistics/1 as a BIF called through the ordinary external call instructions. The entry planner now supports that target. This change reuses the C# register match planner and adds no library or dependency. [Branch emitter](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.EntryClauseFrames.cs) · [Register match planner](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.RegisterPatternCalls.cs).

The permanent deterministic fixture covers two tuple projections with different argument order, an external tail without a match, wrong tuple arity, non-tuple results, and errors from the producing call. Its C# image and OTP image have SHA-256 5A74B5ABE2C3B794CCFF0499C3D161331FC493958BCBED6854ADD8085267DC33. The previous C# image, current image, and OTP agree on 30 values, exceptions, and fixture stack observations. [Fixture source](../../../tests/Erlang.Tests/Resources/Beam/beam_entry_tuple_tail_probe.erl) · [Reference image](../../../tests/Erlang.Tests/Resources/Beam/beam_entry_tuple_tail_probe.beam) · [Runtime comparison](fixture.txt).

## Commands

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1517-final-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1517-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1517-final-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1517-final.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
$tests = 'artifacts/beam-backend/part1517-tests/Erlang.Tests.dll'
dotnet $tests --case compiler/beam/atom-case-emits-local-tails --case compiler/beam/documentation-payload-matches-otp --case compiler/beam/rebuild-installed-otp-release-images --case compiler/beam/whole-module-matches-otp --case compiler/beam/call-result-bif-keeps-register --case compiler/beam/call-result-case-keeps-register --report artifacts/beam-backend/part1517-focused.json
dotnet $tests --block compiler --report artifacts/beam-backend/part1517-compiler.json
```

## Complete corpus

| Check | Result |
| --- | --- |
| Source admission and emission | 1,366 of 1,366. Zero source rejections and zero emission failures |
| Full image equality | 386 Equal and 980 Different. ELDAPv3 remains Equal from C# ASN.1 generation |
| Changed outputs | 19 images and 21 function bodies. The aggregate Code size gap drops by 878 bytes |
| Frame size | 13 functions reach the OTP frame size. Another four move closer. None move farther |
| Normalized instructions | appmon_info:get_sample/1 and xref:start/1 gain equality. No function loses it in this part |
| OTP loader | Both images load in all 980 differing pairs |
| .NET checks | Both builds succeed. Six focused checks pass. The compiler block has 3,722 passes and the same 108 failures out of 3,830 |

[Corpus](otp-corpus.json) · [Status transitions](status-changes.txt) · [Frame delta](frame-delta.txt) · [Function delta](normalized-functions.txt) · [Loader and Code](loader-and-code.txt) · [Focused checks](focused.json) · [Compiler block](compiler-test-status.txt) · [Compiler hashes](sha256.txt).

The remaining tuple-tail scan has 124 functions in 15 modules. Binary entry patterns account for 92 of those functions in four modules, including 80 guarded clauses. The scan identifies functions with the source shape and a larger maximum frame. Inspect the affected branch separately to prove its frame contributes to the difference. The four images with one normalized function difference still have additional chunk differences. [Tuple-tail scan](remaining-tuple-tails.txt) · [Scan script](remaining-tuple-tails.escript) · [Nearest instruction matches](nearest.txt) · [appmon_info](appmon-get-sample.txt).

The full .NET suite was not rerun. The compiler block report is partial and cannot replace the complete test input required for canonical readiness. Publication remains pending.
