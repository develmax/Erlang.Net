# Part 1529 — Preserve guarded argument types through protected calls

The existing local argument analysis now visits the protected expression, normal clauses, handlers and cleanup of try. It also visits the operand of catch. These constructs still have unknown result types in this analysis. When a caller's guard proves an atom, integer or list, that information can reach a private function's saved operand in a terminal tuple projection. The is_list fact is kept separate from proper-list evidence because OTP accepts cons cells with arbitrary tails.

The change reuses the local argument fixed point and type writer. The reference is OTP 29.1.1 at ad05823719d77c8faee87348ea39513d4e2f99c5, including beam_ssa_type argument joins. No dependency was added.

The focused oracle compares complete normalized instructions for three private functions. It checks integer and list annotations and an untyped operand with mixed callers. The probe's surrounding protected callers still differ as complete BEAM code. [Probe source](beam_protected_entry_type.erl) · [Oracle](compare-protected-entry.escript) · [Output](probe.txt).

## Commands

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1529-verified-tool
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1529-verified-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1529-verified.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
```

## Results

All 1,366 release sources were accepted and generated. The complete corpus has 387 Equal and 979 Different, with no status transitions. ELDAPv3 remains Equal from C# ASN.1 generation. OTP loads all 979 differing images. [Corpus](otp-corpus.json) · [Loader](loader-and-code.txt).

Twelve release images and 18 function bodies changed. Five functions reached normalized instruction equality, including both Windows network lookups in net. The independent probe matches 72 protected-call results or errors, and three focused compiler tests pass. The full runtime suite and canonical readiness were not rerun. [Function delta](normalized-functions.txt) · [Probe](probe.txt) · [Focused tests](focused.json).
