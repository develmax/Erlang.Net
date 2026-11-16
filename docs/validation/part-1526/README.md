# Part 1526 — Omit proven atom tests in tuple case alternatives

After a failed tuple test, the compiler can omit the exact comparison when a resolved call has exactly one possible non-tuple result. The shared result contract covers lists:keyfind/3, maps:find/2 and maps:take/2. The projection emitter still tests other atom alternatives. This uses the pinned OTP 29.1.1 specs and release instructions, with no new dependency.

The deterministic fixture exercises both the eliminated comparison and a comparison that must remain. OTP and C# produce identical bytes and agree on 80 values, errors and fixture stack observations. [Fixture](../../../tests/Erlang.Tests/Resources/Beam/beam_tuple_case_known_atom.erl) · [Hashes](sha256.txt) · [Oracle](fixture.txt).

## Commands

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1526-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1526-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1526-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1526-final.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
```

## Results

All 1,366 source files were accepted and generated. The full comparison has 387 Equal and 979 Different, with no status transitions. ELDAPv3 still matches from the C# ASN.1 generator. OTP loads all 979 differing images. [Corpus](otp-corpus.json) · [Loader and Code](loader-and-code.txt).

The change reaches 11 modules and 13 functions. Twelve functions reach normalized instruction equality. The Code size gap drops by 76 bytes. Three focused tests pass. The full runtime suite and canonical readiness were not rerun. [Function delta](normalized-functions.txt) · [Focused tests](focused.json).
