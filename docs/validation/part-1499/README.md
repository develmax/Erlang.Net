# Part 1499 — Share return blocks reached by fallthrough

The compiler now admits identical return blocks with sequential predecessors. Existing label operands are redirected to the retained copy. When removing a copy would interrupt fallthrough, the compiler emits a jump at that position and uses the same relocated label as the explicit branches. Function entries and functions with catch or try setup remain excluded. Pinned OTP `beam_jump:eliminate_fallthroughs` supplies the reference. No dependency was added. [Code edit and block selection](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.SharedReturns.cs).

The pinned OTP VM executes 1,116 variable-use queries against `core_lib`. OTP and both C# images agree on all returns and errors. The six duplicate simple returns in `vu_expr/2` disappear. The current layout still uses 23 jumps to simple returns, compared with 18 before and zero in OTP. [Disassembly totals and execution](core-lib.txt).

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1499-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1499-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1499-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1499.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
dotnet artifacts/beam-backend/part1499-tests/Erlang.Tests.dll --case compiler/beam/documentation-payload-matches-otp --case compiler/beam/rebuild-installed-otp-release-images --case compiler/beam/whole-module-matches-otp --case compiler/beam/call-result-bif-keeps-register --case compiler/beam/call-result-case-keeps-register --report artifacts/beam-backend/part1499-focused.json
dotnet artifacts/beam-backend/part1499-tests/Erlang.Tests.dll --block compiler --report artifacts/beam-backend/part1499-compiler.json
```

Five focused checks pass with the same compiler binary used for the corpus. [Focused results](focused.json) · [Compiler hashes](compiler-hash.txt).

The complete Windows OTP 29.1.1 comparison admits all 1,366 sources and produces 1,366 images. It records 385 Equal and 981 Different, including Equal for the C# ASN.1 generated `ELDAPv3`. OTP loads both images in all 981 differing pairs. [Corpus](otp-corpus.json) · [Transitions](status-changes.txt) · [Loader](loader-and-code.txt).

The optimizer changes 284 images and 629 function bodies, with the measured frame sizes preserved. The Code size gap drops by 4,547 bytes. The excess framed return count falls from 2,056 to 957. Full normalized instruction equality statuses are preserved. The affected modules differ only in Code, function tables, and the derived Attr checksum. [Code and frame totals](summary.txt) · [Normalized functions](normalized-functions.txt) · [Remaining groups](return-eligibility.txt) · [Chunk delta](chunk-delta.txt).

The reachable instruction graph comparison covers 33,442 functions in the changed images. It pairs functions in emission order, resolves labels and jumps, and normalizes the checksum-derived closure identity. Both C# images have the same reachable instruction graphs under that normalization. The OTP VM also executes 3,476 escape-key queries against both C# images and the release image with identical outcomes. Eight duplicate returns disappear from `edlin_key:get_valid_escape_key/2`, while the number of jumps to simple returns grows from 27 to 34. OTP uses none in this function. [Graph results](reachable-graph.txt) · [Escape-key results](edlin-key.txt).


The compiler test block passes 3,721 of 3,829 checks. The same 108 checks fail in the Part 1498 baseline. The full .NET suite was not rerun. Canonical readiness and publication remain pending. The readiness delta uses the immutable Part 561 baseline. [Compiler block](compiler-tests.json) · [Test status comparison](compiler-test-status.txt).

The [graph comparison script](reachable-graph.escript) accepts the previous and current generated-image directories and the LF-delimited changed-image list. The [execution probe](fallthrough-probe.escript) accepts the pinned OTP install and those same two generated-image directories, followed by `core_lib` or `edlin_key`.
