# Part 1540 — Build a measured record field in registers

A function that constructs a binary from the size and value of one record field now validates the input in X0, reads the field into X0 and measures it into X1. The binary construction uses both registers and the OTP integer and bitstring operand types. The badrecord branch keeps the original argument. The emitter shares the measured record pattern with the general binary construction path and adds no dependency.

The deterministic fixture covers byte and bit measurements, byte-aligned and unaligned payloads, and malformed inputs. The complete module matches OTP 29.1.1 byte for byte. All 12 return, error and stack observations agree, including the four previously different badrecord stack lines. [Source](../../../tests/Erlang.Tests/Resources/Beam/beam_measured_record.erl) · [Hashes](sha256.txt) · [Instructions](fixture-instructions.txt) · [Oracle](fixture.txt).

## Reproduce

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1540-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1540-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1540-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1540.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
```

## Corpus results

All 1,366 sources were accepted and generated. The comparison records 388 Equal and 978 Different. All release images have the same bytes as in part 1539. ELDAPv3 remains byte-identical from C# ASN.1 generation. OTP accepts all 978 differing images for loading. Three targeted tests passed. The full runtime suite and canonical readiness were not rerun. [Corpus](otp-corpus.json) · [Delta](normalized-functions.txt) · [Tests](focused.json).
