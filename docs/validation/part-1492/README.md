# Part 1492 — Keep nested case branch calls in registers

The register case planner flattens nested call operands with the existing call sequence lowering. For a tagged arm, it projects the field into X0 when the sequence reads it. The branch keeps only saved arguments that must survive its calls and emits a tail call for its final result. The failed tag or arity check can still return the original selector. The pinned `beam_ssa_pre_codegen` liveness and register reservation passes and the release disassembly provide the reference. [Call planner](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.RegisterCaseCallSequence.cs#L14) · [Field selection](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.GeneralCatch.cs#L1645).

The permanent `tagged_nested_tail/2` fixture checks the converted field and saved argument, an unmatched tuple with the same tag and different arity, and a nested conversion failure. The pinned OTP VM agrees with both C# builds on the tested values and `badarg`. C# now uses one stack slot and `call_last`, as OTP does. The tuple test still differs because OTP infers the tag from `lists:keyfind/3`. [Fixture](../../../tests/Erlang.Tests/Resources/Beam/beam_entry_result_case.erl#L139) · [Execution and disassembly](probe.txt).

The previous checkpoint's broader compiler test block completed with 3,721 passed and 108 failed out of 3,829. Every selected test retained its status from the Part 1482 full suite. [Previous build test comparison](part1491-compiler-block.txt).

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1492-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1492-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1492-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1492.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
dotnet artifacts/beam-backend/part1492-tests/Erlang.Tests.dll --case compiler/beam/documentation-payload-matches-otp --case compiler/beam/rebuild-installed-otp-release-images --case compiler/beam/whole-module-matches-otp --case compiler/beam/call-result-bif-keeps-register --case compiler/beam/call-result-case-keeps-register --report artifacts/beam-backend/part1492-focused.json
```

All 1,366 Windows OTP 29.1.1 sources are admitted and generate a BEAM. The byte comparison records 385 Equal and 981 Different. `ELDAPv3` remains Equal after C# ASN.1 generation. OTP accepts both images in all 981 differing pairs. [Corpus](otp-corpus.json) · [Status transitions](status-changes.txt) · [Loader](loader-and-code.txt).

Fifty-seven release images and 72 function bodies change. Eighteen functions across 15 modules gain instruction equality after label normalization, with no losses. Twenty-one measured frames reach the OTP size. Two previously matching frame sizes become smaller than OTP's in `http_request:key_value_str/2` and `net_adm:collect_host_nodes/2`. The absolute Code size gap drops by 648 bytes. [Normalized comparison](normalized-functions.txt) · [Frame changes](frame-delta.txt) · [Totals](summary.txt).

The instructions of `xmerl_xlate:file/3` now match OTP. The module still differs in `string/3` and `hook/2`. The source scan finds terminal nested cases with call selectors in 691 functions across 318 modules. This is a candidate count for the next compiler change. [Previous instructions](cases-before.txt) · [Current instructions](cases-after.txt) · [Source candidates](source-cases.txt).

Five focused checks pass on the same compiler binary used by the corpus tool. The current build did not run the broader compiler block or the full .NET suite. Canonical readiness and publication remain pending. The readiness delta baseline remains the immutable Part 561 snapshot. [Focused tests](focused.json) · [Compiler hashes](compiler-hash.txt).
