# Part 1494 — Fold nested identity cases and retain fields across the inner call

The register case planner emits a tail call when every inner branch returns the selector and the final fresh-variable arm covers the remaining values. The proof checks variable bindings and tuple reconstruction in field order. The planner also keeps an outer field needed after the inner call by reusing a saved slot whose previous value is dead after that call. It uses the existing call argument mover to preserve the previous slot value when it feeds the selector. The pinned OTP Core fold and SSA liveness passes provide the reference. [Compiler](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.GeneralCatch.cs#L1805).

The permanent controls cover returning an error tuple intact, the outer fallback, and reading the outer list field in both branches after the inner call. The pinned OTP VM and both C# builds agree on the tested values. The identity control uses the same tail call as OTP. The retained-field control reuses Y0 and has a two-slot frame, while OTP reserves a separate slot and uses a three-slot frame. [Fixtures](../../../tests/Erlang.Tests/Resources/Beam/beam_entry_result_case.erl#L157) · [Execution and disassembly](probe.txt).

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1494-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1494-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1494-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1494.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
Copy-Item artifacts/beam-backend/part1494-tool/Erlang.Compiler.dll artifacts/beam-backend/part1494-tests/Erlang.Compiler.dll -Force
dotnet artifacts/beam-backend/part1494-tests/Erlang.Tests.dll --case compiler/beam/documentation-payload-matches-otp --case compiler/beam/rebuild-installed-otp-release-images --case compiler/beam/whole-module-matches-otp --case compiler/beam/call-result-bif-keeps-register --case compiler/beam/call-result-case-keeps-register --report artifacts/beam-backend/part1494-corpus-binary-focused.json
```

All 1,366 Windows OTP 29.1.1 sources are admitted and generate a BEAM. The byte comparison records 385 Equal and 981 Different. `ELDAPv3` remains Equal after C# ASN.1 generation. OTP accepts both images in each differing pair. [Corpus](otp-corpus.json) · [Status transitions](status-changes.txt) · [Loader](loader-and-code.txt).

Six release images and six function bodies change. `test_server_node:start_which_node/1` regains normalized instruction equality. The entry frame reaches the OTP size in `dbg_ieval:rec_clauses/3` and `erl_eval:match_clause/5`. Frames in `dialyzer_utils:'-merge_types/2-fun-0-'/3` and `snmpm_config:verify_user_behaviour/1` become one slot smaller than OTP. The absolute Code size gap drops by 107 bytes. [Restored instructions](identity-release.txt) · [Frames](frame-delta.txt) · [Totals](summary.txt).

The initial fixture used an external `lists:last/1` import outside the dependency selection of the .NET fixture. The final fixture uses the local `pass/1` call and both the OTP and .NET checks pass. A captured local was renamed during the corpus build. The rebuilt DLL has identical method IL and names the generated field `spillSlot` instead of `slot`. The five focused tests pass with each compiler binary, including the exact DLL used for the full corpus. The full .NET suite was not rerun. Canonical readiness and publication remain pending. The readiness delta uses the immutable Part 561 baseline. [Focused tests](focused.json) · [Compiler comparison](compiler-build-difference.txt) · [Compiler hashes](compiler-hash.txt).
