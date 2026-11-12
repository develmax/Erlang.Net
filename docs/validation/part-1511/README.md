# Part 1511 — Emit local tail calls from atom cases

For a case on an entry register with distinct atom patterns, the compiler emits a value dispatch and schedules each local tail call directly in X registers. It substitutes the proven atom when a branch forwards the selector. The fallback allocates its nested tagged exit reason in one heap reservation. An already bound fallback variable retains its exact comparison and the original case-clause failure edge. The compiler resolves the fallback through the ordinary call binding rules before emitting an Erlang exit. [Compiler](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.RegisterPatternCalls.cs).

The reference is OTP 29.1.1 at `ad05823719d77c8faee87348ea39513d4e2f99c5`. The change reuses the entry-call planner, register move scheduler, and tagged-exit emitter. OTP's `beam_ssa_codegen` switch, call and move emission supply the reference. The change adds no dependency.

The deterministic fixture includes dispatch from X0 and X1, reordered atom clauses, forwarding the proven selector, and an already bound fallback. The C# image and OTP image have identical bytes. All six focused tests pass. [Fixture source](../../../tests/Erlang.Tests/Resources/Beam/beam_atom_tail_dispatch_probe.erl) · [Reference BEAM](../../../tests/Erlang.Tests/Resources/Beam/beam_atom_tail_dispatch_probe.beam) · [Focused checks](focused.json) · [Hashes](sha256.txt).

```powershell
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1511-atom-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1511-final-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1511-final.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
dotnet artifacts/beam-backend/part1511-atom-tests/Erlang.Tests.dll --case compiler/beam/atom-case-emits-local-tails --case compiler/beam/documentation-payload-matches-otp --case compiler/beam/rebuild-installed-otp-release-images --case compiler/beam/whole-module-matches-otp --case compiler/beam/call-result-bif-keeps-register --case compiler/beam/call-result-case-keeps-register --report artifacts/beam-backend/part1511-focused.json
dotnet artifacts/beam-backend/part1511-atom-tests/Erlang.Tests.dll --block compiler --report artifacts/beam-backend/part1511-compiler.json
```

The final corpus tool uses the `Erlang.Compiler.dll` from the successful test build. The fixture CLI uses the same DLL.

The final Windows OTP 29.1.1 corpus admits all 1,366 sources and emits all 1,366 images. 386 are Equal and 980 are Different. `AlgorithmInformation-2009` transitions from Different to Equal. `ELDAPv3` remains Equal using the C# ASN.1 source generator. OTP accepts both images in all 980 differing pairs. [Full report](otp-corpus.json) · [Status transitions](status-changes.txt) · [Loader](loader-and-code.txt).

Six images change. All 28 changed function bodies gain normalized instruction equality and reach the OTP frame size, moving from two slots to zero. The aggregate Code size gap falls by 1,633 bytes. The 432 calls to these functions agree with OTP on returned values, exit reasons, and stack frames from the tested modules. 132 previously differing module stacks now agree. [Instruction delta](normalized-functions.txt) · [Frame delta](frame-delta.txt) · [Execution probe](enum-execution.escript) · [Execution results](enum-execution.txt).

The same source-shape audit across the remaining 980 differing images finds zero remaining oversized frames in this group. Four of those images have one normalized instruction body difference each. [Atom audit](atom-tails-after.txt) · [Nearest images](nearest.txt).

The compiler block passes 3,722 of 3,830 tests. The new fixture passes and all 108 pre-existing failures have the same statuses. The formatter wrapped the new test registration after the run. The full .NET suite was not rerun, and the Part 561 comparison baseline remains in use. Canonical readiness and publication remain pending. [Compiler tests](compiler-tests.json) · [Test changes](compiler-test-status.txt).

The corpus CLI returned 1 because 980 byte differences remain. The closest candidates include one differing function in prim_buffer and one in ssh_bits. [prim_buffer instructions](prim-buffer-read_iovec.txt) · [ssh_bits instructions](ssh-bits-mpint.txt).
