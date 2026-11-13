# Part 1532 — Forward tuple fields into terminal tail calls

The terminal tuple-case emitter now recognizes a tail call that reads one projected field and any required original arguments. The selector saves only the inputs read by the arms or the pattern checks. After the checks, the projection goes directly into its destination X register and the tail call loads any saved arguments directly from Y.

The change reuses the existing entry call, tuple projection and tail-call emitters. The reference is OTP 29.1.1 at ad05823719d77c8faee87348ea39513d4e2f99c5. No dependency was added.

The deterministic fixture covers recursive lookup, the original-argument atom alternative, malformed tuples and invalid input. The complete fixture matches OTP and 24 result, error and fixture-stack observations agree. [Fixture](../../../tests/Erlang.Tests/Resources/Beam/beam_case_projected_tail.erl) · [Oracle](compare-projected-tail.escript) · [Hashes](sha256.txt).

## Commands

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1532-verified-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1532-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1532-verified-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1532-verified.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
```

## Results

All 1,366 sources were accepted and generated. The complete comparison records 387 Equal and 979 Different. ELDAPv3 remains Equal from C# ASN.1 generation. OTP loads all 979 differing images. [Corpus](otp-corpus.json) · [Loader and Code](loader-and-code.txt).

The final compiler changes nine release images. Six functions match normalized OTP instructions and six frames reach OTP sizes. The aggregate Code size gap drops by 18 bytes. Projecting into the final argument register restores the five Observer function matches lost in the intermediate build. Three focused checks passed. The full runtime suite and canonical readiness were not rerun. [Function delta](normalized-functions.txt) · [Frame delta](frame-delta.txt) · [Observer instructions](observer-line-head.txt) · [Focused checks](focused.json).
