# Part 1518 — Release stack slots before terminal tuple matches

The general clause emitter now reuses the register tuple-match plan after patterns and guards succeed. This covers terminal clauses where a static call produces a tuple of fresh variables and the tail arguments use only the projected fields and constants. The emitter loads the producer arguments, trims the dead clause frame, checks the result in X0, and projects the live fields into outgoing registers. Error paths preserve the original badmatch value and source location. Rejected patterns and guards still have their original frame for the next clause.

The implementation uses the existing register projection, call operand ordering, and failure emitters. The pinned reference is OTP 29.1.1 at ad05823719d77c8faee87348ea39513d4e2f99c5. OTP computes live intervals and register reservations in beam_ssa_pre_codegen. The current C# plan still opens the general frame before patterns and trims it after the successful guard. OTP can open a frame only on a branch that calls another function. No dependency was added. [General emitter](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.GeneralEmitter.cs) · [Register match planner](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.RegisterPatternCalls.cs).

The permanent runtime check covers two binary arms, field permutation, badmatch, and fallback. A separate oracle comparison checks 63 returns, exceptions, and fixture stack observations in the original OTP, previous C#, and current C# images. [Runtime fixture](../../../tests/Erlang.Tests/Resources/Beam/beam_entry_result_case.erl) · [Test](../../../tests/Erlang.Tests/Compiler/Beam/BeamModuleEmissionChecks.cs) · [Oracle comparison](fixture.txt) · [Fixture instructions](fixture-disassembly.txt).

## Commands

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1518-final-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1518-final-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1518-final-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1518-verified.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
$tests = 'artifacts/beam-backend/part1518-final-tests/Erlang.Tests.dll'
dotnet $tests --case compiler/beam/atom-case-emits-local-tails --case compiler/beam/documentation-payload-matches-otp --case compiler/beam/rebuild-installed-otp-release-images --case compiler/beam/whole-module-matches-otp --case compiler/beam/call-result-bif-keeps-register --case compiler/beam/call-result-case-keeps-register --report artifacts/beam-backend/part1518-final-focused.json
dotnet $tests --block compiler --report artifacts/beam-backend/part1518-final-compiler.json
```

## Complete corpus

| Check | Result |
| --- | --- |
| Source admission and emission | 1,366 of 1,366. Zero source rejections and zero emission failures |
| Full image equality | 386 Equal and 980 Different. ELDAPv3 remains Equal from C# ASN.1 generation |
| Changed outputs | 8 images and 115 normalized function bodies. The combined Code size gap falls by 2,799 bytes |
| Frame sizes | 73 functions move closer to the OTP maximum. None reach it in this part or move farther |
| Normalized instruction equality | No function gains or loses it in this part |
| OTP loader | Both images load in all 980 differing pairs |
| Focused checks | Six of six pass, including the .NET binary tuple-tail checks |

[Corpus](otp-corpus.json) · [Status transitions](status-changes.txt) · [Frame delta](frame-delta.txt) · [Normalized function comparison](normalized-functions.txt) · [Loader and Code](loader-and-code.txt) · [Focused checks](focused.json) · [Compiler hashes](sha256.txt).

The Latin-1 xmerl function parse_pi_1/2 now uses five maximum slots instead of eight. The guarded success branch trims all five before calling whitespace/3. Its tuple projections and tail-call frame size match OTP. The function still opens its frame before dispatch and repeats match starts between binary clauses. [Release disassembly](xmerl-parse-pi.txt).

The corpus scan groups the first normalized instruction difference in each function. In 891 images it finds a function whose first difference is between two allocate instructions. In 885 images at least one such instruction has a larger C# stack size and the same live X-register count. For prim_buffer, tftp_logger, and counters, every differing function begins with this category. Use those images to verify the effect of a shared frame rule on near matches. Later instructions and other chunks still require separate comparison. [Before](first-difference-before.txt) · [After](first-difference-after.txt) · [Near images](nearest-frames.txt) · [Scan script](first-difference-families.escript).

The remaining tuple-tail scan still finds 124 functions across 15 modules whose maximum frame exceeds OTP. The count tracks whether a frame is oversized and stays the same when a frame decreases but remains above the reference. [Remaining tuple tails](remaining-tuple-tails.txt).

The compiler block reports 3,722 passes and the same 108 failures in 3,830 tests. The permanent fixture explicitly declares its lists dependency. The initial harness rejected lists:last/1 because that dependency was absent. The final focused and compiler reports use the corrected harness. The full .NET suite and canonical readiness were not rerun. [Compiler block](compiler-test-status.txt).
