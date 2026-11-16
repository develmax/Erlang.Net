# Part 1507 — Plan entry calls followed by tuple matches

The general entry planner recognizes a flat tuple match against the first call in a sequence. It uses the existing argument scheduler to feed that call directly from the entry registers. It reserves Y slots for arguments used by the pattern or continuation. Tuple fields that are never read need no extraction. The match takes its input from X0 and uses existing tagged and arity tests when the shape cannot be proved. A failure still reports the original result as `badmatch`. The existing call continuation can forward a projected field in X0 if the field is consumed by the next call and does not survive it. [Entry plan](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.GeneralEmitter.cs) · [Tuple binding](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.RegisterPatternCalls.cs).

The reference is pinned OTP `beam_ssa_pre_codegen` for stack frames and live intervals, and the release disassembly for `xmerl_xsd_type:duration_atoms_time/1`. The change reuses capture-based reference analysis, tuple patterns and continuation emission. No dependency was added.

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1507-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1507-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1507-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1507-final.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
dotnet artifacts/beam-backend/part1507-tests/Erlang.Tests.dll --case compiler/beam/documentation-payload-matches-otp --case compiler/beam/rebuild-installed-otp-release-images --case compiler/beam/whole-module-matches-otp --case compiler/beam/call-result-bif-keeps-register --case compiler/beam/call-result-case-keeps-register --report artifacts/beam-backend/part1507-focused.json
dotnet artifacts/beam-backend/part1507-tests/Erlang.Tests.dll --block compiler --report artifacts/beam-backend/part1507-compiler.json
```

Both builds pass with no warnings or errors. Five focused tests pass. The corpus, test harness, and fixture tools use the same compiler binary. [Focused tests](focused.json) · [Compiler hashes](compiler-hash.txt).

The fixture covers known and unknown tuple shapes, exact equality against a bound argument, a literal tag, and unused fields. The unknown-shape function moves from five frame slots to one, matching OTP. Both other fixture functions preserve the frame sizes used by OTP. All 195 returns, error reasons, and module frames in exception stacks agree across OTP and both C# images. [Fixture](beam_first_tuple_match.erl) · [Oracle](tuple-match-probe.escript) · [Results](fixture.txt).

The Windows OTP 29.1.1 comparison accepts all 1,366 sources and generates all 1,366 images. There are 385 Equal and 981 Different with no status transitions. `ELDAPv3` remains Equal after C# ASN.1 generation. OTP loads both images in all 981 differing pairs. [Corpus](otp-corpus.json) · [Transitions](status-changes.txt) · [Loader](loader-and-code.txt).

There are 506 changed function bodies across 227 images. Frames move closer to OTP in 410 functions, including eight that reach its size. None move farther. The absolute Code size gap drops by 8,484 bytes. The set of normalized function matches stays the same. [Frame delta](frame-delta.txt) · [Sizes](summary.txt) · [Normalized functions](normalized-functions.txt).

`asn1ct_check:extract_field/3` moves from seven frame slots to five and retains shape checks. `xmerl_xsd_type:duration_atoms_time/1` moves from seven to six against OTP's two. The 16 xmerl inputs agree across OTP and both C# images. [ASN.1 instructions](asn1ct-extract-field.txt) · [xmerl instructions](xmerl-duration-atoms.txt) · [xmerl probe](xmerl.txt).

The straight-line first-call audit drops from 1,318 functions in 449 modules to 1,024 functions in 397 modules. Its leading tuple match against a local call drops from 285 functions to 42. Among candidates with source information, the largest remaining group is a terminal operator expression in 53 modules. A projected field used by the next tuple-producing call is a separate candidate in 35 modules. The nearest-file ranking identifies `prim_buffer:read_iovec/2` as the only remaining normalized body difference in that image. [Entry saves](first-call-spills-after.txt) · [Tuple chains](tuple-chains-after.txt) · [Nearest files](nearest.txt).

The compiler block passes 3,721 of 3,829 tests, with the same 108 failures as Part 1506. The SDK formatter was applied to the new code's layout after testing. The full .NET suite was not rerun. Canonical readiness and publication remain pending. The readiness delta retains the immutable Part 561 baseline. [Compiler block](compiler-tests.json) · [Test status comparison](compiler-test-status.txt).
