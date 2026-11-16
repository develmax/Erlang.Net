# Part 1493 — Reuse the register plan for a terminal nested case

The register case planner now handles a terminal branch that selects on another call. It projects the outer tuple field directly into the inner argument register when the call uses the field once. Capture analysis identifies the saved inputs used by the inner branches. After loading the call arguments, the compiler clears the other saved slots, leaving the outer alternative with the original frame and selector. The implementation reuses the register case planner and follows pinned OTP `beam_ssa_pre_codegen` and the branch restrictions in `beam_trim`. [Case plan](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.GeneralCatch.cs#L1805) · [Emission](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.GeneralCatch.cs#L2165).

The `tagged_nested_case/3` fixture exercises the inner selector with a projected field and a saved token, both inner returns with a separate saved value, and the outer mismatch with the same tag and a different arity. The pinned OTP VM and both C# builds agree on the tested values. The C# entry frame uses two slots, down from three, matching the OTP size. It projects the field into X1 and clears Y0 before the call, as OTP does. The outer tuple test still differs because OTP infers the key tag from `lists:keyfind/3`. [Fixture](../../../tests/Erlang.Tests/Resources/Beam/beam_entry_result_case.erl#L146) · [Execution and instructions](probe.txt).

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1493-checked-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1493-checked-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1493-checked-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1493-checked.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
dotnet artifacts/beam-backend/part1493-checked-tests/Erlang.Tests.dll --case compiler/beam/documentation-payload-matches-otp --case compiler/beam/rebuild-installed-otp-release-images --case compiler/beam/whole-module-matches-otp --case compiler/beam/call-result-bif-keeps-register --case compiler/beam/call-result-case-keeps-register --report artifacts/beam-backend/part1493-checked-focused.json
```

All 1,366 Windows OTP 29.1.1 sources are admitted and generate a BEAM. The byte comparison records 385 Equal and 981 Different. `ELDAPv3` remains Equal after C# ASN.1 generation. OTP accepts both images in each differing pair. [Corpus](otp-corpus.json) · [Status transitions](status-changes.txt) · [Loader](loader-and-code.txt).

Forty-seven images and 53 function bodies change. Nine functions reach normalized instruction equality and 19 frames reach the OTP size. The absolute Code size gap drops by 1,017 bytes. One previously matching body, `test_server_node:start_which_node/1`, now emits a redundant identity case after its local call. The next change needs to fold the reconstruction back to the selector on branches that preserve it. [Normalized instructions](normalized-functions.txt) · [Frame totals](summary.txt) · [Previous instructions](cases-before.txt) · [Current instructions](cases-after.txt).

The source scan finds 171 candidate functions in 118 modules that reference a variable from the outer pattern in the inner branches, and 119 functions in 90 modules with a computed inner selector argument. These counts identify shapes for further lowering. The previous generated images show 31 candidate `lists:keyfind/3` tuple-check mismatches in 24 modules. [Nested case candidates](source-cases.txt) · [Lookup type candidates](keyfind-before.txt).

Five focused checks pass with the same compiler binary as the corpus tool. The current build did not run the broader compiler block or the full .NET suite. Canonical readiness and publication remain pending, with Part 561 as the immutable readiness delta baseline. [Focused checks](focused.json) · [Compiler hashes](compiler-hash.txt).
