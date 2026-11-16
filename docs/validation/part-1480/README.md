# Part 1480 — Forward measured call results and arithmetic guards in X

The branch entry emitter recognizes a resolved unary measurement BIF applied to the immediately preceding call result. It passes the result in x0 and writes the measurement directly into its Y slot. It saves the original value after the BIF only when the continuation reads it. The guard emitter compares a simple arithmetic result directly from x0 when the other operand is already bound or literal. Both changes reuse the existing BIF and comparison lowering.

The prim_buffer read_iovec/2 large-size branch now uses gc_bif for byte_size and compares the subtraction result in X. Its remaining stack size is seven slots, compared with four in OTP. In the permanent control, arithmetic comparisons move from a three-slot frame to two slots. OTP keeps the arithmetic in X and has no frame. The measurement control still uses Y slots to build its tuple, while OTP uses X.

The pinned OTP VM and both C# versions agree on the tested values, byte_size badarg, and failure to satisfy an arithmetic guard. The prior corpus source ranking identifies 329 mismatching functions in 126 modules with a simple arithmetic guard comparison, and 80 functions in 42 modules with a producer followed by a unary measurement. The counts describe source forms, including cases outside the current emission paths.

- [Measurement lowering](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.EntryClauseFrames.cs) and [guard lowering](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.GeneralEmitter.cs)
- [Fixture](../../../tests/Erlang.Tests/Resources/Beam/beam_entry_result_case.erl), [probe](probe.txt), [prim_buffer](prim-buffer-read_iovec.txt), and [source ranking](guard-bif-ranking-before.txt)

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1480-checked-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1480-checked-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1480-checked-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1480-checked.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
dotnet artifacts/beam-backend/part1480-checked-tests/Erlang.Tests.dll --case compiler/beam/documentation-payload-matches-otp --case compiler/beam/rebuild-installed-otp-release-images --case compiler/beam/whole-module-matches-otp --case compiler/beam/call-result-bif-keeps-register --case compiler/beam/call-result-case-keeps-register --report artifacts/beam-backend/part1480-checked-focused.json
```

The complete Windows OTP 29.1.1 comparison generates all 1,366 release images with 385 Equal and 981 Different. There are zero rejected sources, unsupported modules, compiler errors and whole-file status transitions. ELDAPv3 remains Equal after C# ASN.1 generation. OTP accepts both images in all 981 differing pairs.

The change affects 149 images and 399 normalized instruction bodies. Frame sizes move closer to OTP in 287 functions, including two that reach the OTP size. The aggregate Code size gap falls by 1,500 bytes. The set of equal normalized instruction bodies stays the same.

- [Full corpus](otp-corpus.json), [status transitions](status-changes.txt), and [OTP loader](loader-and-code.txt)
- [Frame totals](summary.txt), [normalized instructions](normalized-functions.txt), and [remaining guard and BIF ranking](guard-bif-ranking-after.txt)
- [Dead producer probe](dead-result-probe.txt), [focused tests](focused.json), and [compiler hashes](compiler-hash.txt)

All five focused checks pass using the final corpus compiler binary. The full .NET suite was not rerun. Canonical readiness and publication remain pending. The readiness comparison baseline remains the immutable Part 561 snapshot.
