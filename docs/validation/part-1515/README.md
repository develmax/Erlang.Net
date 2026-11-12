# Part 1515 — Narrow dispatch with finite atom arguments

The compiler traces private entry arguments through direct call sites and forwarded parameters. It can read finite choices from preceding bindings and terminal case or if arms. Exported, native, indirect and unknown paths block specialization. The analysis treats variable arguments inside nested binding scopes as unknown. OTP limits finite atom sets to five values. The same limit keeps larger sets represented as atoms without a finite value list.

For exhaustive literal functions, one plan specifies the reachable arms and their label count. The emitter returns a constant when all reachable arms agree, uses an exact test for two distinct arms, and otherwise emits a smaller table with one reachable arm as the default. Tail-call branches use the proven two-way test or direct call. [Argument proof](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.FiniteAtomArguments.cs) · [Label planning](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.cs) · [Literal emission](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.Lowering.cs).

The reference is OTP 29.1.1 at `ad05823719d77c8faee87348ea39513d4e2f99c5`. The change follows OTP's call argument facts and the release dispatch layouts. It reuses the typed register, exact comparison, and call emitters. No dependency was added.

The deterministic fixture matches OTP byte for byte. It covers direct and forwarded closed calls, reduced tables, two-way literal and tail-call branches, constant returns, the six-value limit case, and function-clause failures on unknown input. It leaves an otherwise unreachable helper exported so the fixture also isolates dispatch from whole-function reachability. OTP and both C# builds agree on 57 fixture observations and 16 release megaco timestamp returns. Both builds and all six focused checks pass. [Fixture](../../../tests/Erlang.Tests/Resources/Beam/beam_finite_atom_probe.erl) · [OTP fixture BEAM](../../../tests/Erlang.Tests/Resources/Beam/beam_finite_atom_probe.beam) · [Fixture results](fixture.txt) · [Megaco results](megaco.txt) · [Hashes](sha256.txt).

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1515-capped-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1515-capped-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1515-capped-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1515-capped.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
dotnet artifacts/beam-backend/part1515-capped-tests/Erlang.Tests.dll --case compiler/beam/atom-case-emits-local-tails --case compiler/beam/documentation-payload-matches-otp --case compiler/beam/rebuild-installed-otp-release-images --case compiler/beam/whole-module-matches-otp --case compiler/beam/call-result-bif-keeps-register --case compiler/beam/call-result-case-keeps-register --report artifacts/beam-backend/part1515-capped-focused.json
dotnet artifacts/beam-backend/part1515-capped-tests/Erlang.Tests.dll --block compiler --report artifacts/beam-backend/part1515-capped-compiler.json
```

The capped Windows OTP 29.1.1 comparison admits all 1,366 sources and emits all 1,366 images. 386 are Equal and 980 are Different, with no status transitions. `ELDAPv3` remains Equal using the C# ASN.1 source generator. OTP loads both images in all 980 differing pairs. The corpus CLI returns 1 for the remaining byte differences. [Corpus](otp-corpus.json) · [Transitions](status-changes.txt) · [Loader](loader-and-code.txt).

Seven images and 13 functions change. All 13 functions gain normalized instruction equality. The first run without the five-atom limit lost three previously matching function bodies. The final run restores all three images to their previous SHA-256 and keeps the 13 gains. The aggregate Code size gap falls by 164 bytes, and frame sizes remain the same. [Function delta](normalized-functions.txt) · [Pre-limit delta](pre-cap-function-delta.txt) · [Restored images](cap-restored.txt) · [Sizes](summary.txt).

The previous selector audit identified 16 functions for this work. Thirteen now match normalized OTP instructions. The remaining examples are `prim_tty:move/3`, `appmon_info:get_sample/1`, and `httpc_manager:select_session/4`. OTP keeps queue as the default arm in get_sample. The first two cases need the general clause emitter to use the proof. The third needs the finite return values of session_type propagated through a binding. [Dispatch shapes](dispatch-shapes.txt) · [Nearest files](nearest.txt).

The compiler block passes 3,722 of 3,830 tests with the same 108 failures as Part 1514. The full .NET suite was not rerun. Canonical readiness and publication remain pending. The readiness comparison retains the immutable Part 561 snapshot. [Compiler tests](compiler-tests.json) · [Test statuses](compiler-test-status.txt).
