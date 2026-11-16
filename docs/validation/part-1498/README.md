# Part 1498 — Share identical return blocks within a function

The compiler identifies blocks that end in `deallocate; return`, optionally preceded by a move into X0. It shares blocks with identical operands and frame sizes, retaining the later block as in the reverse traversal of OTP `beam_jump:share_1`. Function entries, fallthrough, and functions with catch or try setup are excluded. The existing instruction decoder provides the byte ranges of compact label operands, including operands in dispatch tables. The compiler applies the label map to Code, exports, locals, and closures before computing the module checksum. No dependency was added. [Optimizer](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.SharedReturns.cs) · [Decoder](../../../src/Runtime/Erlang.Runtime.Core/Execution/Beam/BeamInstructionReader.cs) · [Assembly](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.cs#L869).

The three changed functions in `beam_bounds` no longer contain any of their previous duplicate simple return blocks, matching OTP on this measure. The pinned OTP VM executes 2,592 input combinations across `bounds/3`, `relop/3`, and `infer_relop_types/3`. The original and both C# images agree on every observed return or error. [Disassembly](beam-bounds-returns.txt) · [Execution](beam-bounds-values.txt).

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1498-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1498-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1498-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1498.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
dotnet artifacts/beam-backend/part1498-tests/Erlang.Tests.dll --case compiler/beam/documentation-payload-matches-otp --case compiler/beam/rebuild-installed-otp-release-images --case compiler/beam/whole-module-matches-otp --case compiler/beam/call-result-bif-keeps-register --case compiler/beam/call-result-case-keeps-register --report artifacts/beam-backend/part1498-focused.json
dotnet artifacts/beam-backend/part1498-tests/Erlang.Tests.dll --block compiler --report artifacts/beam-backend/part1498-compiler.json
```

Five focused checks pass with the same compiler binary used for the corpus. [Focused results](focused.json) · [Compiler hashes](compiler-hash.txt).

The complete Windows OTP 29.1.1 comparison admits all 1,366 sources and produces 1,366 images. It records 385 Equal and 981 Different, including Equal for the C# ASN.1 generated `ELDAPv3`. Both files in every differing pair load on OTP. [Corpus](otp-corpus.json) · [Transitions](status-changes.txt) · [Loader](loader-and-code.txt).

The optimizer changes 213 images and 419 function bodies without changing their measured frame sizes. Four functions gain normalized instruction equality and none lose it. The aggregate absolute Code size gap drops by 5,000 bytes. The number of excess repeated simple return blocks drops from 2,759 to 2,056. Of the remaining excess, 1,099 are in functions where a repeated block is adjacent to a possible fallthrough or label, and 957 are in functions with exception setup. [Normalized functions](normalized-functions.txt) · [Code and frame totals](summary.txt) · [Before](return-blocks-before.txt) · [After](return-blocks-after.txt) · [Remaining groups](return-eligibility.txt).


An independent payload comparison confirms that changes are confined to Code, the three function tables, and the derived Attr checksum. The compiler test block passes 3,721 of 3,829 checks. All 108 failures retain the same statuses as in Part 1491. The full .NET suite was not rerun. Canonical readiness and publication remain pending. The readiness delta uses the immutable Part 561 baseline. [Chunk delta](chunk-delta.txt) · [Compiler block](compiler-tests.json) · [Test status comparison](compiler-test-status.txt).
