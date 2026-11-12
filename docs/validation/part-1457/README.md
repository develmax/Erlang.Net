# Part 1457 — Emit literal-result tables with guarded and failing fallbacks

The C# emitter reuses its literal dispatch table for unary function clauses and a terminal case on the original argument. Constant branches with the same result share a block. The fallback preserves x0 for a type guard, an identity return, or case_clause with the case expression's location. Constant erlang:error/1 fallbacks use the existing tail-call emitter.

Four focused Erlang fixtures cover a guarded case, an incomplete case, a guarded function clause and a constant error. The fixture oracle compares normalized instructions and executes both images. The release oracle compares the 34-key inet_dns:encode_type/1 and 50-key Diameter avp_header/1 bodies, and checks the generated images with DNS and Diameter inputs.

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1457-tool-final
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1457-tool-final/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1457-final.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
```

- [Fixture instructions and execution](probe.txt), [focused tests](focused.json), and [compiler hash](compiler-hash.txt)
- [DNS instructions](dns-encode-type.txt), [Diameter instructions](diameter-header.txt), and [release execution](release-execution.txt)

The full Windows release run admitted all 1,366 sources and generated all 1,366 images. It records 383 Equal and 983 Different, with zero status changes. ELDAPv3 still matches after C# ASN.1 generation. Both loaders accepted all 983 differing images.

Eighteen images changed and 33 functions gained normalized instruction equality with zero losses. Thirty-two functions now have the OTP frame size. The aggregate Code chunk size gap decreased by 2,697 bytes.

The refreshed scan finds 146 literal-return table candidates in 70 images. Grouping by the OTP table's failure target identifies 32 functions in 21 images with a literal default. The group includes ssl_handshake:key_exchange_alg/1, where OTP folds atom equality guards into the table and drops the key whose result equals the default. Cases such as inet_dns:encode_opcode/1 also remain when the fallback guard refers to the outer input through a wildcard pattern.

- [Full corpus](otp-corpus.json) and [status transitions](status-changes.txt)
- [Normalized instruction changes](normalized-functions.txt), [frame changes](frame-delta.txt), and [loader and Code chunk checks](loader-and-code.txt)
- [Entry branch shapes](entry-branch-shapes.txt), [literal fallback ranking](literal-fallback-ranking.txt), and [SSL guard instructions](ssl-atom-guards.txt)
