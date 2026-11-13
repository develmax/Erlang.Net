# Part 1549 — Trim call sequences that return a constant

The call-chain emitter now handles sequences of discarded calls followed by a constant. It keeps only arguments that survive the first call in the stack frame and trims slots after their last use. Ordinary-call lowering also accepts the returning BIFs demonitor/1, demonitor/2 and exit/2.

The normalized instructions for snmpm_server:nis_handle_pong_timeout/2 match OTP. The standalone probe checks two successful calls, a failing call and the process dictionary state after each. [Disassembly](snmpm_server.txt) · [Probe](beam_discarded_calls.erl) · [Observed outcomes](fixture.txt).

## Reproduce the corpus comparison

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1549-tool
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1549-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1549.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
```

## Corpus results

The C# compiler accepted and generated all 1,366 modules. The comparison found 390 Equal and 976 Different. ELDAPv3 remains byte-identical from C# ASN.1 generation. Seventeen images changed, with nine newly matching functions in seven modules and no lost matches after label normalization. OTP accepted all 976 differing images for loading. Three repository regression checks passed. [Corpus](otp-corpus.json) · [Function comparison](normalized-functions.txt) · [Loader results](loader-and-code.txt) · [Tests](focused.json).
