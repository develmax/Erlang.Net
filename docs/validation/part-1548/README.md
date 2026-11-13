# Part 1548 — Allocate tuple fields by lifetime

For an unguarded tuple entry followed by ordinary calls, the emitter projects fields read by the first call into X registers and saves only fields used after that call in Y slots. It reuses the call chain's last-use tracking and stack trimming.

The instructions of asn1ct_imm:enc_cg_cond_1/1 match OTP after label normalization. Its stack frame drops from two slots to one. The independent fixture checks fields read before and after calls, process dictionary effects, failure in the first call, and function clause errors for invalid input. All five observations matched on the OTP VM. Three repository regression checks passed. [Disassembly](asn1ct_imm.txt) · [Fixture](beam_tuple_call_lifetimes.erl) · [Results](fixture.txt) · [Tests](focused.json).

## Reproduce the corpus comparison

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1548-final-tool
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1548-final-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1548.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
```

## Corpus results

The C# compiler accepted and generated all 1,366 modules. The comparison found 390 Equal and 976 Different. Only asn1ct_imm changed, gaining one instruction match after label normalization. ELDAPv3 remains byte-identical from the C# ASN.1 generator. The OTP loader accepted all 976 different images. [Corpus](otp-corpus.json) · [Function comparison](normalized-functions.txt) · [Loader results](loader-and-code.txt).
