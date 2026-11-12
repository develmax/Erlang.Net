# Part 1516 — Apply finite atom arguments to general entry branches

The general entry emitter uses a finite argument set when every possible value has an unguarded literal arm with no additional equality. It emits reachable bodies in descending atom order and uses the last reachable body as the default. Two possible values use an exact comparison, while a single value enters its body directly. The finite argument analysis also follows a direct local call when its successful return expressions prove a finite set of atoms. [Entry lowering](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.EntryClauseFrames.cs) · [Finite argument proof](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.FiniteAtomArguments.cs).

The reference is OTP 29.1.1 at `ad05823719d77c8faee87348ea39513d4e2f99c5`. The change reuses the existing finite atom limit, general branch plans, typed register writer, and comparison emitter. No dependency was added.

The deterministic fixture matches OTP byte for byte. It exercises general dispatch with nested calls and a locally returned atom bound before a second call. The previous and current C# images agree with OTP on 57 return, exception, and module-stack observations. Both builds and all six focused checks pass. [Fixture source](../../../tests/Erlang.Tests/Resources/Beam/beam_finite_entry_dispatch_probe.erl) · [OTP fixture BEAM](../../../tests/Erlang.Tests/Resources/Beam/beam_finite_entry_dispatch_probe.beam) · [Fixture results](fixture.txt) · [Hashes](sha256.txt) · [Focused checks](focused.json).

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1516-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1516-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1516-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1516-final.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
dotnet artifacts/beam-backend/part1516-tests/Erlang.Tests.dll --case compiler/beam/atom-case-emits-local-tails --case compiler/beam/documentation-payload-matches-otp --case compiler/beam/rebuild-installed-otp-release-images --case compiler/beam/whole-module-matches-otp --case compiler/beam/call-result-bif-keeps-register --case compiler/beam/call-result-case-keeps-register --report artifacts/beam-backend/part1516-focused.json
dotnet artifacts/beam-backend/part1516-tests/Erlang.Tests.dll --block compiler --report artifacts/beam-backend/part1516-compiler.json
```

## Complete corpus

| Check | Result |
| --- | --- |
| Release images | All 1,366 sources admitted and compiled. 386 Equal and 980 Different. ELDAPv3 remains Equal |
| OTP loader | Both images load in all 980 differing pairs |
| Changed images and functions | 22 images and 27 function bodies changed. Two functions gained normalized instruction equality, and one closure helper lost it. The combined Code size gap fell by 70 bytes |
| Compiler block | 3,722 passed and 108 failed out of 3,830, with the same test statuses as Part 1515 |

[Corpus](otp-corpus.json) · [Status changes](status-changes.txt) · [Function changes](normalized-functions.txt) · [Loader and Code](loader-and-code.txt) · [Compiler block comparison](compiler-test-status.txt).

The normalized instructions now match for prim_tty:move/3 and httpc_manager:select_session/4. In appmon_info:get_sample/1, the table now matches OTP with two explicit keys and queue as the default. Two branches still allocate three stack slots where OTP uses registers. The changed branch emission order in the wx notification example assigns the first closure failure helper to source line 95. OTP uses line 83. [prim_tty](prim-tty-move.txt) · [httpc_manager](httpc-select-session.txt) · [appmon_info](appmon-get-sample.txt) · [wx closure helpers](wx-closure-helpers.txt).

The previous corpus contains 223 functions in 43 modules with a tuple match after a call, a later static tail call, and a larger frame than OTP. In 127 functions across 17 modules, the tail arguments use only fields of the tuple and literals. These counts identify candidates for inspecting register lifetimes. [Broad scan](tuple-tail-before.txt) · [Fields and literals](tuple-tail-no-entry.txt).

The complete .NET suite was last run separately. Canonical readiness and publication remain pending.
