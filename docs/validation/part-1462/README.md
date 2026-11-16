# Part 1462 — Share field returns across tagged tuple clauses

The grouped tuple entry emitter now handles unary dispatch by tuple arity and atom tag when each matching clause returns one bound field. Each distinct field has a shared return block. The block reads the field directly into x0, and the original input remains in x0 for any failed match. A constant fallback is supported, as is the normal function_clause path. Duplicate tags within an arity retain the first clause.

A fixture with three tuple arities and shared fields agrees with OTP on decoded instructions after removing source lines. Both images load in OTP and agree on seven values, including wrong tuple arity and a non-tuple fallback. The permanent C# test executes the same cases. Three focused .NET cases passed against the compiler used by the corpus run.

In asn1ct, get_name_of_def/1 drops from 72 to 21 instructions and get_pos_of_def/1 drops from 89 to 27. Both now match OTP's decoded instructions after label normalization. beam_types:get_bs_matchable_unit/1 and diameter_sctp:id/1 also match after normalization.

- [Control fixture](beam_grouped_tuple_projection.erl), [decoded instructions and results](probe.txt), and [ASN.1 comparisons](asn1ct-projections.txt)
- [Focused tests](focused.json) and [compiler hash](compiler-hash.txt)

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1462-tool
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1462-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1462.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
```

The full OTP 29.1.1 corpus admits all 1,366 sources and produces 1,366 images. It records 383 Equal and 983 Different, with zero status transitions. ELDAPv3 remains Equal using the C# ASN.1 generator. Both loaders accept the 983 differing images. Ten images change. Fifteen functions gain normalized instruction equality and none lose it. All 17 changed frame sizes reach the OTP value, and the aggregate Code size gap falls by 969 bytes.

The remaining adjacent projection and move candidates most often lead to another move, an external call, or a local call. These continuations occur in 403, 328 and 280 images respectively. The sets overlap. A follow-up needs the argument register dependencies to determine whether an earlier projection can write directly to its destination.

- [Corpus](otp-corpus.json), [status changes](status-changes.txt), [normalized function comparison](normalized-functions.txt), [frame summary](summary.txt) and [loader results](loader-and-code.txt)
- [Strict fixture](beam_grouped_tuple_strict.erl), [duplicate clauses and failure arguments](strict-projection.txt)
- [Continuation ranking](projection-next-op.txt), [before](projection-continuations-before.txt), [after](projection-continuations-after.txt) and [scanner](rank-projection-continuations.escript)

After building, the layout checker normalized the final line ending in the new C# file. The full .NET suite and canonical readiness validation remain pending.
