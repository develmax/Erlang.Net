# Part 1460 — Emit single-clause fold callbacks as closures

The C# BEAM emitter now uses its existing closure and lists:foldl/3 call path for anonymous callbacks with one clause. The callback is created before the accumulator and list are evaluated. An exception raised in the callback retains a separate callback frame.

The emitter also recognizes a captured fold over constant list input followed by reversal and record-field append. It evaluates the constant list subtraction, saves the record in one slot, and emits the closure and tail append using the original locations. The control fixture exercises single-clause and multi-clause folds, a captured record and a callback error.

The release body of http_response:header_list/1 matches OTP after label and closure identity normalization at 18 instructions, down from 67. Both images return the same tested headers and the same HTTP error frames. The lifted callback still uses a second frame slot in C# and lacks OTP's typed result operand.

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1460-tool-checked
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1460-tool-checked/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1460-checked.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
```

- [Fixture instructions and execution](probe.txt), [HTTP entry instructions](header-fold-entry.txt), and [HTTP execution](header-execution.txt)
- [Remaining lifted callback differences](header-callback.txt)

The full corpus admitted all 1,366 sources and generated 1,366 images. The comparison records 383 Equal and 983 Different, with zero status transitions. ELDAPv3 remains Equal after C# ASN.1 generation. Both loaders accepted all differing images. Three focused .NET cases passed.

The change affects 138 images. It brings 102 functions to normalized instruction equality. One function, ct_framework:save_seq/4, loses that equality because the closure index changes from 9 to 10. The frame size moves closer to OTP for 237 functions and farther for 150. The aggregate Code size gap decreases by 7,750 bytes.

The count of functions with fewer direct lists:foldl/3 calls than OTP drops from 239 in 139 images to 12 in seven. All seven remaining images belong to the compiler application. In core_lint, OTP inlines local callers such as expr_list/3, while C# retains the call. The release profile records inline, but the code-generation entry uses only the source module's inline settings. The corpus has 56 differing images that record the release inline option.

- [Full corpus](otp-corpus.json), [focused tests](focused.json), and [compiler hash](compiler-hash.txt)
- [Normalized instructions](normalized-functions.txt), [frame comparison](frame-delta.txt), and [closure index difference](save-seq-closure-index.txt)
- [Remaining fold calls](fold-call-ranking.txt), [release inline inventory](inline-profile-ranking.txt), [Core lint calls](core-lint-fold.txt), and [Core lint options](core-lint-options.txt)
