# Part 1481 — Project known binary split results and construct measured tuples in X

When a resolved `erlang:split_binary/2` call succeeds, the result is a two-element tuple. The compiler projects only the fresh fields read by the continuation. It keeps the standard pattern matching path for calls with unresolved result shapes, repeated fields, and previously bound fields. The branch entry emitter can also build a terminal tuple directly from the registers holding a call result and its unary measurement.

In `prim_buffer:read_iovec/2`, the C# compiler reduces the main frame from seven slots to five. The release compiler uses four slots and extracts the first field directly into x0 before building the list. The `measure/2` control moves from two Y slots to zero and matches the reference instruction sequence after normalizing labels. The `split_prefix/2` control drops from six Y slots to four. OTP and both C# builds agree on the tested values, the branch bypassing the split, and `badarg` from invalid input. [Release instructions](prim-buffer-read_iovec.txt) · [OTP control](probe.txt).

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1481-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1481-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1481-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1481.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
dotnet artifacts/beam-backend/part1481-tests/Erlang.Tests.dll --case compiler/beam/documentation-payload-matches-otp --case compiler/beam/rebuild-installed-otp-release-images --case compiler/beam/whole-module-matches-otp --case compiler/beam/call-result-bif-keeps-register --case compiler/beam/call-result-case-keeps-register --report artifacts/beam-backend/part1481-focused.json
```

The Windows OTP 29.1.1 corpus generates all 1,366 release images. There are 385 Equal and 981 Different, with zero rejected sources, unsupported modules, or compiler errors. ELDAPv3 remains Equal after C# ASN.1 generation. OTP accepts both images in all 981 differing pairs. [Corpus](otp-corpus.json) · [Status transitions](status-changes.txt) · [Loader](loader-and-code.txt).

Ten release images and twelve normalized instruction bodies change. Ten frame sizes move closer to OTP. No new release image or normalized function body reaches equality. The aggregate absolute Code size gap drops by 469 bytes. `prim_buffer` has a smaller frame and a larger absolute Code size gap because its generated Code section was already shorter than OTP's before the change. [Instruction comparison](normalized-functions.txt) · [Frame totals](summary.txt) · [Chunk ranking](chunk-ranking.txt).

Five focused tests pass with the same compiler binary used by the corpus run. The full .NET suite remains unverified for this checkpoint. Canonical readiness and publication remain pending. [Focused tests](focused.json) · [Compiler hashes](compiler-hash.txt).
