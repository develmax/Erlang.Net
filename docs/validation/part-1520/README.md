# Part 1520 — Project retained tuple fields before their calls

A two-step call sequence can forward its first result in X0 and keep the next result there while checking both nested tuples. The generator retains the live inner tuple in one slot and reads each field directly into a register before its unary call. The last field read frees the tuple slot for the previous call result. The final tuple and list cells use one heap check. Shape failures still report badmatch with the original outer tuple.

The plan recognizes the call and tuple shapes independently of the module and function names. It applies to two or more retained fields. It uses the existing call planner, tuple tests, and frame instructions. The pinned reference is OTP 29.1.1 at ad05823719d77c8faee87348ea39513d4e2f99c5. No dependency was added. [Compiler implementation](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.NestedTupleFieldCalls.cs).

The independent deterministic fixture matches OTP with SHA-256 5FF6867599C1D898B04E249644E251CD66E0528F5BDF53914439CC56189F34F9. The oracle checks 126 results, exceptions, and fixture stack observations for two, three, and four retained fields. The isolated release check uses the unmodified tftp_logger source and BEAM in their release-relative paths. The corpus tool reports Equal for the whole image. [Exact fixture](../../../tests/Erlang.Tests/Resources/Beam/beam_nested_tuple_field_calls.erl) · [OTP fixture](../../../tests/Erlang.Tests/Resources/Beam/beam_nested_tuple_field_calls.beam) · [Oracle comparison](fixture.txt) · [Isolated release check](tftp-release.json).

## Commands

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1520-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1520-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1520-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1520-final.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
$tests = 'artifacts/beam-backend/part1520-tests/Erlang.Tests.dll'
dotnet $tests --case compiler/beam/atom-case-emits-local-tails --case compiler/beam/documentation-payload-matches-otp --case compiler/beam/rebuild-installed-otp-release-images --case compiler/beam/whole-module-matches-otp --case compiler/beam/call-result-bif-keeps-register --case compiler/beam/call-result-case-keeps-register --report artifacts/beam-backend/part1520-focused.json
dotnet $tests --block compiler --report artifacts/beam-backend/part1520-compiler.json
```

## Complete corpus

| Check | Result |
| --- | --- |
| Source admission and emission | 1,366 of 1,366. Zero source rejections and zero emission failures |
| Full image equality | 387 Equal and 979 Different. ELDAPv3 remains Equal from C# ASN.1 generation |
| Changed outputs | Only tftp_logger.beam changes, from Different to Equal |
| Function and frame | add_timestamp/2 moves from 13 to 4 slots and reaches normalized instruction equality |
| Code size | tftp_logger moves from 454 to 419 bytes, matching OTP |
| OTP loader | Both images load in all 979 differing pairs |
| Focused checks | Six of six pass |

[Corpus](otp-corpus.json) · [Status transitions](status-changes.txt) · [Frame delta](frame-delta.txt) · [Loader and Code](loader-and-code.txt) · [Focused checks](focused.json) · [Compiler hashes](sha256.txt).

The deterministic CLI disassembly displays an empty source filename. The release-profile comparison verifies the complete Windows image, including its source metadata. [CLI disassembly](tftp-cli-disassembly.txt) · [Release-profile result](tftp-release.json).

The remaining scan finds 1,120 functions with a tuple arm in a case on a call result and a larger first allocate with equal live inputs. The largest identified terminal group returns a variable from the tuple in an unguarded arm. It covers 249 functions in 129 images. The report ranks nearby examples, including i:im/0 and rb_format_supp:get_opt/2. [Before](tuple-case-frame-shapes-before.txt) · [After](tuple-case-frame-shapes-after.txt) · [Script](tuple-case-frame-shapes.escript) · [Nearest images](nearest.txt).

The compiler block reports 3,722 passes and the same 108 failures in 3,830 tests. The full .NET suite and canonical readiness were not rerun. [Compiler block](compiler-test-status.txt).
