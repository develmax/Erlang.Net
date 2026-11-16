# Part 1476 — Construct case branch call arguments in X

The register tuple arm emitter uses the existing call argument construction planner for tuple and list expressions. It reads pattern fields from X registers and accesses enclosing bindings from their existing slots. The planner emits one heap check for the constructed arguments. The ordinary and control transfer call targets use the existing call classification. Calls keep their existing tail position.

The permanent nested case fixture reaches the OTP frame size of one slot, down from two. OTP and both C# images agree on the tested return and error values. A separate control with two constructed arguments, repeated fields, a tuple alias and an enclosing binding reduces the C# frame from five slots to one and agrees with OTP. The release xmerl_xsd_re map/1 reduces its frame from one slot to zero. OTP projects the shared field before dispatch and emits a tail error call. Both instruction differences remain in the current image.

- [Compiler](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.GeneralEmitter.cs), [fixture source](../../../tests/Erlang.Tests/Resources/Beam/beam_entry_result_case.erl), and [control instructions](probe.txt)
- [Release before](../part-1475/map-selector-after.txt), [release after](map-selector-after.txt), and [runtime comparison](runtime.txt)

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1476-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1476-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1476-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1476.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
dotnet artifacts/beam-backend/part1476-tests/Erlang.Tests.dll --case compiler/beam/documentation-payload-matches-otp --case compiler/beam/rebuild-installed-otp-release-images --case compiler/beam/whole-module-matches-otp --case compiler/beam/call-result-bif-keeps-register --case compiler/beam/call-result-case-keeps-register --report artifacts/beam-backend/part1476-focused.json
```

The prior Part 1475 images include 100 differing functions in 76 modules with at least two unguarded tagged two-element tuple arms that each read their payload. These are candidates for sharing field extraction before dispatch. The classification counts source shapes. Each function still requires instruction comparison to identify where its emitter differs.

- [Instruction ranking](shared-payload-code-ranking.txt) and [frame ranking](shared-payload-frame-ranking.txt)

The Windows OTP 29.1.1 comparison generates all 1,366 release images, with 384 Equal and 982 Different. There are zero rejected sources, unsupported modules, compiler errors and whole-file status transitions. ELDAPv3 remains Equal after C# ASN.1 generation. The pinned OTP VM accepts both images in all 982 differing pairs.

The change affects 166 images and 305 normalized instruction bodies. Frame sizes move closer to OTP in 153 functions, including 61 that reach the OTP frame size. The aggregate Code size gap falls by 2,195 bytes. The set of matching normalized bodies stays the same.

- [Full corpus](otp-corpus.json), [status changes](status-changes.txt), and [loader](loader-and-code.txt)
- [Frame totals](summary.txt), [normalized instructions](normalized-functions.txt), and [focused checks](focused.json)

All five focused checks pass using the corpus compiler binary. The full .NET suite was not rerun. Canonical readiness and publication remain pending. The readiness comparison baseline remains the immutable Part 561 snapshot.
