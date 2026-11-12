# Part 1474 — Place frames after unary overload guards

The unary overload emitter accepts a guarded static command followed by an object command. The static clause tests the scalar before stack allocation and handles the missing environment on its own path. The object clause checks its pattern before preserving the receiver. Both use existing emitters and keep the same argument registers, line locations and tail calls as the pinned reference.

The pinned Windows OTP 29.1.1 wxDisplay source and image are embedded in the release image check. The source retains its original copyright and license header. An independent compile from the installed source produces the same SHA-256 as the release image. The selection is based on the clause structure and command call expressions.

- [Compiler](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.WxObjectCommands.cs)
- [Fixture source](../../../tests/Erlang.Tests/Resources/Beam/wxDisplay.erl), [release check](../../../tests/Erlang.Tests/Compiler/Beam/BeamReleaseImageChecks.cs), [hashes](wxDisplay-hashes.txt), and [new/1 instructions](wxDisplay-new.txt)

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1474-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1474-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1474-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1474.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
dotnet artifacts/beam-backend/part1474-tests/Erlang.Tests.dll --case compiler/beam/documentation-payload-matches-otp --case compiler/beam/rebuild-installed-otp-release-images --case compiler/beam/whole-module-matches-otp --case compiler/beam/call-result-bif-keeps-register --case compiler/beam/call-result-case-keeps-register --report artifacts/beam-backend/part1474-focused.json
```

The prior Part 1473 images were grouped by the source form associated with the first oversized allocate instruction. The largest group is a single unguarded clause with variable parameters and a case body. It has 1,758 functions in 485 modules with a nonzero OTP frame. Direct remote call selectors account for 757 of those functions across 346 modules. A separate group of 214 functions across 153 modules has a remote call selector and a zero-slot OTP frame. These counts prioritize investigation by coverage. Functions can have additional differences after the first frame allocation.

- [Source forms](frame-source-ranking.txt) and [case selectors](case-selector-ranking.txt)

The complete Windows OTP 29.1.1 comparison generates all 1,366 release images, with 384 Equal and 982 Different. There are zero rejected sources, unsupported modules and compiler errors. wxDisplay is the only changed image and transitions to Equal. Its Code chunk shrinks from 1,370 to 1,306 bytes, matching OTP, and new/1 gains normalized instruction equality. ELDAPv3 remains Equal after the C# ASN.1 generation. The OTP loader accepts both images in all 982 differing pairs.

All five focused .NET checks pass using the same compiler binary as the corpus. The full .NET suite was not rerun. Canonical readiness and publication remain pending. The readiness comparison baseline remains the immutable Part 561 snapshot.

- [Full corpus](otp-corpus.json), [status changes](status-changes.txt), [normalized instructions](normalized-functions.txt), and [loader](loader-and-code.txt)
- [Focused tests](focused.json) and [compiler hashes](compiler-hash.txt)
