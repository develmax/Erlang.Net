# Part 1547 — Keep computed maps across a nested call

A call result can feed a literal-key map and the next call directly from X0. The emitter puts the map in Y0, prepares the nested call arguments, and trims the frame to one slot holding the map. The final local tail call receives the nested call result and the saved map.

The 18 disassembled instructions in format_log/1 match OTP after label normalization in seven release modules. [Comparison](format-log-cohort.txt) · [Example](gen-server-format-log.txt).

A standalone Erlang fixture checks three successful inputs, failures in each of the two calls, and the resulting process dictionary state on the OTP VM. The outputs from both compilers match. Three repository regression checks passed. [Fixture](beam_map_across_call.erl) · [Observed outcomes](fixture.txt) · [Tests](focused.json).

## Reproduce the corpus comparison

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1547-tool
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1547-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1547.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
```

## Corpus results

The compiler accepted and generated all 1,366 modules. The comparison found 390 Equal and 976 Different. ELDAPv3 remains byte-identical from C# ASN.1 generation. Seven images changed, each gaining one matching function and losing none after label normalization. OTP accepts all 976 differing images for loading. [Corpus](otp-corpus.json) · [Function comparison](normalized-functions.txt) · [Loader results](loader-and-code.txt).
