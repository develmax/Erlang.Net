# Part 1519 — Keep singleton results and omit unused tuple bindings

The list case emitter tests the tail of a singleton candidate and reuses the original list when prepending a literal. It keeps the producer result in X0 in both branches. The general plan tests for a nonempty list first. The known return type of erlang:integer_to_list/1 allows that test to be omitted, and its tail receives the proper-list type. The two-argument variant retains the test and uses an untyped tail. OTP 29.1.1 records the one-argument fact in beam_call_types. [List result emitter](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.ListResults.cs).

Discarded nested tuple matches use the existing binding-liveness analysis to skip dead field projections. They retain shape checks, repeated names, outer bindings, and bindings referenced by the continuation. The change adds no dependency. The pinned reference is OTP 29.1.1 at ad05823719d77c8faee87348ea39513d4e2f99c5. [Sequence emitter](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.GeneralEmitter.cs).

The deterministic fixture for both integer conversion arities matches OTP with SHA-256 DE54223FEC97091F2CE558074914342012FEDF549C687CD7CA025FB0F27384C1. The runtime fixture checks nested live fields and repeated binding failures. A standalone oracle compares 108 values, exceptions, and fixture stack observations, including untyped and improper list inputs. [Exact fixture source](../../../tests/Erlang.Tests/Resources/Beam/beam_singleton_conversion_probe.erl) · [OTP image](../../../tests/Erlang.Tests/Resources/Beam/beam_singleton_conversion_probe.beam) · [Runtime fixture](../../../tests/Erlang.Tests/Resources/Beam/beam_entry_result_case.erl) · [Oracle results](fixture.txt).

The abstract-form scan identifies 27 candidate functions in 18 modules. It covers top-level clause sequences in the saved abstract forms. The corpus comparison measures actual emission changes. [Scan](candidates.txt) · [Script](candidate-scan.escript).

## Commands

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1519-release-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1519-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1519-release-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1519-final.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
$tests = 'artifacts/beam-backend/part1519-tests/Erlang.Tests.dll'
dotnet $tests --case compiler/beam/atom-case-emits-local-tails --case compiler/beam/documentation-payload-matches-otp --case compiler/beam/rebuild-installed-otp-release-images --case compiler/beam/whole-module-matches-otp --case compiler/beam/call-result-bif-keeps-register --case compiler/beam/call-result-case-keeps-register --report artifacts/beam-backend/part1519-focused.json
dotnet $tests --block compiler --report artifacts/beam-backend/part1519-compiler.json
```

## Complete corpus

| Check | Result |
| --- | --- |
| Source admission and emission | 1,366 of 1,366. Zero source rejections and zero emission failures |
| Full image equality | 386 Equal and 980 Different. ELDAPv3 remains Equal from C# ASN.1 generation |
| Changed outputs | 19 images and 30 normalized function bodies. The aggregate Code size gap falls by 296 bytes |
| Frame sizes | 27 frames move closer to OTP, including one that reaches its size. None move farther |
| Normalized instruction equality | tftp_logger:t/1 gains equality. No function loses it |
| OTP loader | Both images load in all 980 differing pairs |
| Focused checks | Six of six pass, including both integer conversion arities and the nested binding regression |

[Corpus](otp-corpus.json) · [Status transitions](status-changes.txt) · [Frame delta](frame-delta.txt) · [Function changes](normalized-functions.txt) · [Loader and Code](loader-and-code.txt) · [Focused checks](focused.json) · [Compiler hashes](sha256.txt).

In tftp_logger, the singleton conversion now uses a zero-slot frame, and the Type chunk matches OTP. The only normalized function difference in that module is add_timestamp/2. Its frame decreases from 16 to 13 slots while OTP uses four. The module still differs in Code, Attr, ExpT, Line, and LocT. [t/1](tftp-t.txt) · [add_timestamp/2](tftp-add_timestamp.txt) · [Chunk comparison](tftp-chunks.txt).

The source-shape scan starts from functions with a larger first allocate and equal live X-register count. It finds 778 functions in 306 images with an unguarded variable-only entry clause whose body starts with a case on a call result and uses tuple patterns. It also finds 653 functions in 292 images where an initial result binding is followed by a call that receives no direct operand from that result. A function can contribute multiple shapes when it has multiple clauses. This scan identifies where to check delayed extraction and allocation. [Source-shape scan](large-frame-source-shapes-after.txt) · [Script](large-frame-source-shapes.escript) · [Nearest images](nearest.txt).

The compiler block reports 3,722 passes and the same 108 failures in 3,830 tests. The full .NET suite and canonical readiness were not rerun. [Compiler block](compiler-test-status.txt).
