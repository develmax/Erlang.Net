# Part 1484 — Match tuple aliases and literal fields in entry registers

The separate entry frame planner accepts a variable alias around a tuple and literal fields beyond the tag in eligible tuple dispatch. It preserves the original argument for the alias and for later clauses. The projection paths that can overwrite a tuple input require an unused input and no field tests. The pinned `beam_core_to_ssa` source shows tuple and value dispatch in the OTP compiler. This change reuses the C# tuple projection and exact comparison emitters. [Entry planner](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.EntryClauseFrames.cs).

In `tuple_alias/2`, C# checks the tuple, integer field, and guard in X before allocating the two-slot frame on the matching call branch. The previous compiler allocated four slots at entry. The OTP VM returns the same values from the reference and both C# images for both alias orientations, a failing guard, and an integer literal matched against a float. OTP shares the tuple check and uses `select_val` for the literal fields. [Fixture](../../../tests/Erlang.Tests/Resources/Beam/beam_entry_result_case.erl) · [Instruction and result comparison](probe.txt).

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1484-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1484-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1484-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1484.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
dotnet artifacts/beam-backend/part1484-tests/Erlang.Tests.dll --case compiler/beam/documentation-payload-matches-otp --case compiler/beam/rebuild-installed-otp-release-images --case compiler/beam/whole-module-matches-otp --case compiler/beam/call-result-bif-keeps-register --case compiler/beam/call-result-case-keeps-register --report artifacts/beam-backend/part1484-focused.json
```

All 1,366 Windows OTP 29.1.1 sources are admitted and generate a BEAM. The comparison records 385 Equal and 981 Different, including an Equal `ELDAPv3` from C# ASN.1 generation. OTP accepts both images in each differing pair. [Corpus](otp-corpus.json) · [Status changes](status-changes.txt) · [Loader](loader-and-code.txt).

The change affects 225 images and 362 instruction bodies. Three functions in `public_key` and one in `string` reach normalized instruction equality. Previous matches are retained. Frame sizes move closer to OTP in 237 functions, including 51 that reach its frame size. The aggregate absolute Code size gap increases by 1,500 bytes. The largest increase is `ct_ssh`, whose `handle_msg/2` now uses 34 branch-local allocations like OTP, and still emits 94 tagged tuple tests compared with 36 in OTP. [Normalized matches](normalized-summary.txt) · [Frame totals](summary.txt) · [Public key example](public-key-subject-id.txt) · [Dispatch counts](ct-ssh-dispatch.txt).

The candidate ranking drops from 246 to 200 alias functions and from 167 to 135 functions with literal tuple fields. The shared-dispatch ranking finds 551 modules with consecutive literal entry clauses and 104 with consecutive clauses on the same tagged tuple and different literal field values. These counts describe source shapes among differing functions. [Remaining tuple shapes](tuple-shapes-after.txt) · [Literal dispatch candidates](literal-dispatch-after.txt) · [Nearest release images](nearest-after.txt).

Five focused checks pass with the same compiler binary as the corpus tool. The full .NET suite was not run on this build. The older Part 1482 run has finished with 9,520 passed and 182 failed. Canonical readiness and publication remain pending. The readiness comparison baseline remains the immutable Part 561 snapshot. [Focused checks](focused.json) · [Compiler hashes](compiler-hash.txt).
