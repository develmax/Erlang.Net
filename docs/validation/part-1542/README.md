# Part 1542 — Construct a singleton list directly from a split binary result

When a terminal branch matches split_binary/2 and returns one of its fields in a singleton list, the compiler keeps the returned tuple in X0 through the heap check. It projects the selected field into X0 and immediately builds the list. It reuses the existing validation for the split_binary binding and requires fresh bindings. Calls preceding the match retain their order and effects. OTP 29.1.1 provides the reference and the change adds no dependency.

The probe compares 28 values, error reasons and process dictionary effects with both the previous compiler and OTP. It includes invalid sizes, invalid inputs and a side effect before the call. The fixture retains differences in frame placement and instruction layout. [Source](beam_split_singleton.erl) · [Instructions](fixture-instructions.txt) · [Oracle](fixture.txt).

## Reproduce

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1542-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1542-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1542-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1542.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
```

## Corpus results

All 1,366 sources were accepted and generated. There are 389 byte-identical images and 977 different images. ELDAPv3 remains byte-identical from C# ASN.1 generation. OTP accepts all 977 differing images for loading. The only changed release image is prim_buffer. Its read_iovec/2 frame shrinks from five slots to four, matching the OTP maximum. The new projection and list construction match OTP locally. Branch trimming and operand types still differ. The absolute Code size gap grows from 35 to 36 bytes. Three targeted tests passed. The full runtime suite and canonical readiness were not rerun. [Corpus](otp-corpus.json) · [Instructions](prim-buffer.txt) · [Tests](focused.json).
