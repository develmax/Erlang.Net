# Part 1521 — Return call-result tuple projections in registers

Terminal case arms can test and project a single path through a tuple without saving the selector or its fields in Y slots. One arm can use a constant fallback. Several alternatives can share the outer tuple's arity and payload field when their tags differ. In that case the generator loads the payload once and keeps the original selector in X0 until all tests succeed. A case without a fallback still raises case_clause with the complete original selector.

The compiler reuses the call planner, tuple tests, and frame emission. The pinned reference is OTP 29.1.1 at ad05823719d77c8faee87348ea39513d4e2f99c5. No dependency was added. [Compiler implementation](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.CallTupleCaseProjection.cs).

The deterministic fixture matches OTP with SHA-256 B4D7EAFDBBEE95C8E1FCB3CAD02E32C83B7EB409EA5A0B5AEF7B64C5D57C199D. It covers a shared tag dispatch with a nested projection and a single tagged arm with a constant fallback. The oracle compares 47 values, exceptions, and fixture stack observations with both C# builds. [Exact fixture](../../../tests/Erlang.Tests/Resources/Beam/beam_call_tuple_case_projection.erl) · [OTP fixture](../../../tests/Erlang.Tests/Resources/Beam/beam_call_tuple_case_projection.beam) · [Oracle comparison](fixture.txt).

## Commands

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1521-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1521-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1521-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1521-initial.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
$tests = 'artifacts/beam-backend/part1521-tests/Erlang.Tests.dll'
dotnet $tests --case compiler/beam/atom-case-emits-local-tails --case compiler/beam/documentation-payload-matches-otp --case compiler/beam/rebuild-installed-otp-release-images --case compiler/beam/whole-module-matches-otp --case compiler/beam/call-result-bif-keeps-register --case compiler/beam/call-result-case-keeps-register --report artifacts/beam-backend/part1521-focused.json
dotnet $tests --block compiler --report artifacts/beam-backend/part1521-compiler.json
```

## Complete corpus

| Check | Result |
| --- | --- |
| Source admission and emission | 1,366 of 1,366. Zero source rejections and zero emission failures |
| Full image equality | 387 Equal and 979 Different. tftp_logger and ELDAPv3 remain Equal |
| Changed outputs | Seven images and seven functions change. All seven functions reach normalized instruction equality |
| Frame sizes | All seven reach the OTP frame size of zero slots |
| Code size | The aggregate gap to OTP falls by 74 bytes |
| OTP loader | Both images load in all 979 differing pairs |
| Focused checks | Six of six pass |

[Corpus](otp-corpus.json) · [Status transitions](status-changes.txt) · [Function delta](normalized-functions.txt) · [Frame delta](frame-delta.txt) · [Loader and Code](loader-and-code.txt) · [Focused checks](focused.json) · [Compiler hashes](sha256.txt).

The shared-tag layout matches i:im/0, and the single nested path with a constant fallback matches rb_format_supp:get_opt/2. The remaining modules still differ elsewhere. [i:im/0](im.txt) · [rb_format_supp:get_opt/2](get_opt.txt).

The tuple-case scan now finds 1,113 functions where the first allocate has more slots and the same live X-register count as OTP. The group with an unguarded terminal projection and a final pattern instead of a wildcard contains 103 functions in 77 images. Inspect whether the remaining cases differ in shared tags, tuple arities, or projected fields before choosing the next lowering rule. [Before](tuple-case-fallback-before.txt) · [After](tuple-case-fallback-after.txt) · [Scan script](tuple-case-fallback-shapes.escript) · [Nearest images](nearest.txt).

The compiler block reports 3,722 passes and the same 108 failures in 3,830 tests. The full .NET suite and canonical readiness were not rerun. [Compiler block](compiler-test-status.txt).
