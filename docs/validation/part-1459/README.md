# Part 1459 — Dispatch record fields by atom key

The C# emitter now selects an atom key before testing and projecting a tagged tuple. Branches with the same record tag, arity and field share code. A failed record pattern keeps the original arguments and reaches function_clause.

The emitter also handles keyed record accesses followed by local tail calls. It folds atom_to_list on a bound literal key, keeps a shared return for an explicitly suppressed field value, and preserves the original record access location for badrecord. The parser records that location on the access node.

The normalized release body for httpc_manager:get_option/2 matches at 65 instructions, down from 103. The body for http_response:key_value_tuple/2 matches at 240 instructions, down from 446. Both HTTP images return the same headers for ordinary and suppressed content lengths. They agree on the top badrecord frame. The caller frame still differs because C# inlines the fold in header_list/1 and OTP uses a separate callback.

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1459-tool-checked
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1459-tool-checked/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1459-checked.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
```

- [Fixture instructions and execution](probe.txt), [focused tests](focused.json), and [compiler hash](compiler-hash.txt)
- [Option instructions](option-dispatch.txt), [header instructions](header-dispatch.txt), and [HTTP execution](header-execution.txt)

The full corpus admitted all 1,366 sources and generated all 1,366 images. It records 383 Equal and 983 Different, with zero status transitions. ELDAPv3 remains Equal after C# ASN.1 generation. Both loaders accepted all differing images.

Three images changed and two functions gained normalized instruction equality. All three changed functions now have the OTP frame size. The third, erl_lint:get_doc_attr/2, still needs a caller proof for the record shape and the exhaustive two-key domain before it can omit the tests that OTP eliminates.

The corpus scan finds 239 functions across 139 images with fewer direct lists:foldl/3 calls than OTP. In http_response:header_list/1, OTP creates a closure and calls lists:foldl/3 while C# emits a loop. That also changes the callback frame visible in the HTTP error stack.

- [Full corpus](otp-corpus.json), [instruction changes](normalized-functions.txt), and [loader and Code checks](loader-and-code.txt)
- [Remaining documentation record tests](doc-record-dispatch.txt), [fold call ranking](fold-call-ranking.txt), and [HTTP fold instructions](header-fold.txt)
