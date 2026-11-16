# Part 1456 — Group literal entry clauses with tail-call fallbacks

The C# emitter now uses a jump table when consecutive unguarded clauses match distinct literals in one argument and directly tail-call with preloaded registers and constants. The final variable clause becomes the failure target, with the original arguments still available. Local calls with the same destination and argument preparation share a block. Atom keys are ordered by Erlang term order.

The existing register tail emitter supplies the call preparation and emission. The earlier literal-clause path also tries the table before linear tests. The first candidate added the table to the later emitter alone and left the control fixture unchanged. The final candidate matches OTP on repeated targets, a non-atom key, and a fallback that forwards the key and value.

In megaco_text_parser_v1:yeccpars2_cont_4/7 the 97 comparisons become a table in x1. The body falls from 394 to 299 instructions and matches the release disassembly after rebasing labels. Both parser images return the same term for a valid ServiceChange message and the same syntax error for an incomplete message when loaded in OTP.

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1456-tool-final
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1456-tool-final/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1456-final.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
```

- [Fixture instructions and execution](probe.txt), [parser instructions](parser-dispatch.txt), and [parser execution](parser-execution.txt)
- [Focused .NET tests](focused.json) and [compiler hash](compiler-hash.txt)

All 1,366 sources were admitted and all 1,366 images were generated. The full comparison reports 383 Equal and 983 Different, with zero status transitions. ELDAPv3 remains Equal after C# ASN.1 generation. Both loaders accepted all 983 differing images.

Twenty images changed and 384 functions gained normalized instruction equality with zero losses. The aggregate Code chunk size gap shrank by 2,085 bytes across the changed images. Individual gaps grew in some modules. The remaining tail-call group has 11 functions in nine images.

The entry dispatch ranking also finds 170 functions in 75 images where every target in the OTP atom table returns a literal. In inet_dns:encode_type/1 the fallback tests the original integer in x0 and returns it or raises case_clause. This shape still allocates a frame and checks the atom keys individually in C#.

- [Full corpus](otp-corpus.json) and [status transitions](status-changes.txt)
- [Instruction changes](normalized-functions.txt), [loader and Code chunk checks](loader-and-code.txt), and [remaining function differences](image-function-differences.txt)
- [Entry branch shapes](entry-branch-shapes.txt) and [DNS literal return instructions](dns-literal-return.txt)
