# Part 1533 — Forward multiple tuple fields to terminal calls

The terminal tuple branch can now pass several fields of the same tuple to a tail call. The planner keeps the source tuple register live until the last extraction. When an earlier field targets that register, the existing argument move scheduler restores the call argument order after extraction.

The change reuses the tuple pattern validator, selector save plan and tail-call emitter. OTP 29.1.1 is the reference. No dependency was added.

The deterministic fixture covers two projected arguments and one saved argument. It matches OTP byte for byte and agrees on 48 values, errors and fixture stack observations. [Fixture](../../../tests/Erlang.Tests/Resources/Beam/beam_case_multi_projected_tail.erl) · [Hashes](sha256.txt) · [Oracle output](fixture.txt).

## Commands

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1533-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1533-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1533-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1533-final.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
```

## Release example

The release function `mod_disk_log:transfer_log/7` now uses one Y slot for the input that survives the selector call. The generated branch reads both tuple fields into X registers and uses the same swap and tail call as OTP. The function matches after normalizing label numbers. [Instruction listing](mod-disk-log.txt).

## Results

All 1,366 release sources were accepted and generated. Of the full images, 387 match byte for byte and 979 differ. ELDAPv3 remains byte-identical from C# ASN.1 generation. OTP loads all 979 differing images. [Corpus](otp-corpus.json) · [Loader](loader-and-code.txt).

Three release images change. All three frames reach OTP sizes and the total Code size gap drops by 54 bytes. One function reaches normalized instruction equality. Three focused compiler checks passed. The full runtime suite and canonical readiness were not rerun. [Focused checks](focused.json) · [Function delta](normalized-functions.txt) · [Frame delta](frame-delta.txt) · [First differences](first-difference-ranking.txt).
