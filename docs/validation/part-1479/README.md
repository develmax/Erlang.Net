# Part 1479 — Evaluate comparison guards before allocating branch frames

For multiple clauses with independent variable parameters and a leading ordering comparison, the entry emitter uses the existing branch frame and register call planners. It evaluates the guards against X registers. The second guard is omitted only when the comparisons cover complementary sides of the same numeric limit in the same argument register. A branch that returns a singleton list of an ordinary call result constructs it from x0 in a zero-slot frame.

The prim_buffer read/2 instruction stream matches the Windows OTP 29.1.1 release including labels and line locations. The read_iovec/2 entry test and small-size branch now match. The large-size branch still allocates seven slots compared with four in OTP. The permanent fixture and independent OTP execution agree for integer and float boundaries, an atom ordered after numbers, and a gap between two guards.

- [Compiler](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.EntryClauseFrames.cs), [fixture](../../../tests/Erlang.Tests/Resources/Beam/beam_entry_result_case.erl), and [execution probe](probe.txt)
- [read/2 comparison](prim-buffer-read.txt), [read_iovec/2 comparison](prim-buffer-read_iovec.txt), and [baseline comparison ranking](comparison-ranking-before.txt)

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1479-checked-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1479-checked-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1479-checked-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1479-checked.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
dotnet artifacts/beam-backend/part1479-checked-tests/Erlang.Tests.dll --case compiler/beam/documentation-payload-matches-otp --case compiler/beam/rebuild-installed-otp-release-images --case compiler/beam/whole-module-matches-otp --case compiler/beam/call-result-bif-keeps-register --case compiler/beam/call-result-case-keeps-register --report artifacts/beam-backend/part1479-checked-focused.json
```

The complete Windows OTP 29.1.1 comparison generates all 1,366 images, with 385 Equal and 981 Different. There are zero source rejections, unsupported modules, compiler errors and whole-file status transitions. ELDAPv3 remains Equal after C# ASN.1 generation. The pinned OTP loader accepts both images in all differing pairs.

Seventy images change. Twenty function bodies reach normalized instruction equality and none lose it. Frame sizes move closer to OTP in 42 functions, including 33 that reach the OTP size. The aggregate Code size gap falls by 517 bytes.

- [Full corpus](otp-corpus.json), [status transitions](status-changes.txt), and [loader](loader-and-code.txt)
- [Normalized instructions](normalized-functions.txt), [frame totals](summary.txt), and [remaining comparison guards](comparison-ranking-after.txt)
- [Focused tests](focused.json) and [compiler hashes](compiler-hash.txt)

All five focused checks pass using the final corpus compiler binary. The full .NET suite was not rerun. Canonical readiness and publication remain pending. The readiness comparison baseline remains the immutable Part 561 snapshot.
