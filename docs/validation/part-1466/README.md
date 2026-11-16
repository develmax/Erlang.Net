# Part 1466 — Select closed closures from tuple keys before allocating a frame

The generator shares the tuple shape and common field tests for unary clauses that differ in one atom or integer field and return closed functions. The varying field feeds a select_val table. The closure blocks are emitted in descending key order and preserve their source ordinals. The fallback may return a closed closure or a constant. Without a fallback, the selector uses a scratch register so function_clause receives the original tuple.

In the fixture, pick/1 drops from 48 to 27 instructions, and strict/1 drops from 28 to 19, excluding line operations. Both match the OTP instructions after normalizing the closure hash. The oracle runs verify the selected closures, fallback for a wrong prefix or a float key, and the original unmatched argument in the exception stack.

- [Fixture source](beam_literal_tuple_closures.erl), [decoded instructions](probe.txt), and [instruction counts](probe-summary.txt)

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1466-tool
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1466-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1466.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
```

All 1,366 sources were admitted and generated BEAM. The full comparison records 383 Equal and 983 Different with zero status transitions. ELDAPv3 remains Equal after C# ASN.1 generation. OTP accepts both images of each differing pair. Three focused .NET cases passed against the same compiler binary as the corpus.

Five public_key images changed. All 14 changed functions gained normalized instruction equality and their frame sizes reached the OTP value. The aggregate Code size gap fell by 7,928 bytes. In CMSAesRsaesOaep-2009, both getenc_SMimeCaps/1 and getdec_SMimeCaps/1 drop from 160 to 52 instructions with zero frame allocations, as in OTP.

The current ranking finds 525 modules with a tuple test scheduled differently from OTP around frame allocation, down from 527. The nonempty list group remains at 530 modules. Its examples include unused_bitlist/3, whose empty clause returns a tuple with a computed field and currently prevents the separate-entry planner from using the frameless list branch.

- [Full corpus](otp-corpus.json), [status changes](status-changes.txt), and [OTP loading](loader-and-code.txt)
- [Function comparison](normalized-functions.txt), [frame and Code totals](summary.txt), [CMS closure dispatch](cms-closure-dispatch.txt), and [current frame ranking](frame-ranking.txt)
- [Focused tests](focused.json) and [compiler hash](compiler-hash.txt)

The full .NET suite and canonical readiness validation remain pending.
