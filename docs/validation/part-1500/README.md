# Part 1500 — Share returns after exception scope exit

The compiler follows the local instruction graph in functions with catch or try setup. It tracks handler installation and exit, including slot shifts at frame trim and the exceptional edge to each handler. A return label is eligible only if all observed incoming paths have an empty handler stack. Handler entry labels retain their identity. Ambiguous stack states exclude a label. The shared block still requires equal operands and frame sizes. Pinned OTP `beam_jump:share_1` and `beam_validator:verify_no_ct` provide the reference. The compiler reuses the existing instruction decoder and label relocation. No dependency was added. [Scope analysis](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.ReturnScopes.cs) · [Return selection](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.SharedReturns.cs).

`erl_erts_errors:format_erlang_error/3` loses all 42 excess simple return blocks and retains its nine exception scope instructions. OTP and both C# builds agree on all 4,160 observed returns and errors. [Execution and disassembly totals](erl-erts-errors.txt).

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1500-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1500-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1500-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1500.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
dotnet artifacts/beam-backend/part1500-tests/Erlang.Tests.dll --case compiler/beam/documentation-payload-matches-otp --case compiler/beam/rebuild-installed-otp-release-images --case compiler/beam/whole-module-matches-otp --case compiler/beam/call-result-bif-keeps-register --case compiler/beam/call-result-case-keeps-register --report artifacts/beam-backend/part1500-focused.json
dotnet artifacts/beam-backend/part1500-tests/Erlang.Tests.dll --block compiler --report artifacts/beam-backend/part1500-compiler.json
```

Five focused checks pass with the same compiler binary used for the corpus. [Focused results](focused.json) · [Compiler hashes](compiler-hash.txt).

The complete Windows OTP 29.1.1 comparison admits all 1,366 sources and produces 1,366 images. It records 385 Equal and 981 Different, including Equal for the C# ASN.1 generated `ELDAPv3`. OTP loads both images in all 981 differing pairs. [Corpus](otp-corpus.json) · [Transitions](status-changes.txt) · [Loader](loader-and-code.txt).

The optimizer changes 193 images and 347 function bodies, with measured frame sizes preserved. The aggregate absolute Code size gap drops by 5,635 bytes. The excess framed return count falls from 957 to three. Normalized instruction equality statuses are preserved. Payload changes are confined to Code, function tables, and the derived Attr checksum. [Code and frame totals](summary.txt) · [Normalized functions](normalized-functions.txt) · [Remaining groups](return-eligibility.txt) · [Chunk delta](chunk-delta.txt).

The instruction graph comparison covers 25,419 functions and checks the first emitted instruction at 2,534 handler entries. It pairs functions in emission order, resolves labels and jumps, and normalizes the checksum-derived closure identity. Both C# images agree under that normalization. OTP and both C# images also agree on all 120 tested XML parser results and errors. [Graphs](reachable-graph.txt) · [XML results](xmerl-sax-parser.txt).

In 1,003 functions across 155 modules, normalized instructions agree after removing typed register wrappers. The largest single group is missing integer selector types. The scan finds 306 such operands in 303 functions across 32 modules. [Operand ranking](rank-operands.txt) · [Instruction ranking](rank-types.txt).


The integer selector inventory groups 280 occurrences immediately after a label and 26 immediately after a local call. The next step is to carry proven types across incoming control flow and through local call results. [Selector predecessors](rank-selectors.txt).

The compiler test block passes 3,721 of 3,829 checks. The same 108 checks fail in the Part 1499 baseline. The full .NET suite was not rerun. Canonical readiness and publication remain pending. The readiness delta uses the immutable Part 561 baseline. [Compiler block](compiler-tests.json) · [Test status comparison](compiler-test-status.txt).

The [graph comparison script](reachable-graph.escript) accepts the previous and current generated-image directories and the LF-delimited changed-image list. The [execution probe](exception-probe.escript) accepts the pinned OTP install and those same two generated-image directories, followed by `erl_erts_errors` or `xmerl_sax_parser_utf8`.
