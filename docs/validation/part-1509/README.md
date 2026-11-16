# Part 1509 — Plan append arguments of tail calls

The entry call planner recognizes an append used as a call argument when its operands are literals, entry variables or ordinary calls. It prepares append operands using the call scheduler, then keeps the result in X0 for the terminal call. When two calls supply append operands, the saved call planner keeps the first result until the append and trims expired slots. Both paths preserve source file and line information for the append. [Call chain](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.NestedArgumentTail.cs) · [Saved call sequence](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.SavedCallSequence.cs).

The reference is pinned OTP `beam_ssa_pre_codegen` and the release instructions for SNMP `config_err/2` and `xref_utils:module_filename/2`. The change reuses the existing two call planners and adds no dependency.

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1509-literal-checked-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1509-literal-checked-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1509-literal-checked-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1509-literal-checked.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
dotnet artifacts/beam-backend/part1509-literal-checked-tests/Erlang.Tests.dll --case compiler/beam/documentation-payload-matches-otp --case compiler/beam/rebuild-installed-otp-release-images --case compiler/beam/whole-module-matches-otp --case compiler/beam/call-result-bif-keeps-register --case compiler/beam/call-result-case-keeps-register --report artifacts/beam-backend/part1509-literal-checked-focused.json
dotnet artifacts/beam-backend/part1509-literal-checked-tests/Erlang.Tests.dll --block compiler --report artifacts/beam-backend/part1509-literal-checked-compiler.json
```

The fixture covers append in the first and second argument, repeated use of an input, two append operands computed by calls, and a fully constant append. The constant append has no runtime call. All five frame sizes agree with OTP. Its 203 observations agree with OTP, including four exception stacks where the prior C# image kept an extra frame for the caller. [Fixture](beam_append_tail_argument.erl) · [Oracle](append-tail-probe.escript) · [Results](fixture.txt).

The early implementation lowered a constant append in `test_server_ctrl:copy_html_files/2` to a runtime call and increased its frame. The verified implementation leaves constant expressions to the existing evaluator. This restores the literal `"*.src.html"` and the one-slot frame used by OTP. [Instructions](copy-html-files.txt).

Both final builds pass with no warnings or errors. All five focused tests pass. The corpus, harness, and fixture tools use the same compiler hash. [Focused tests](focused.json) · [Compiler hashes](compiler-hash.txt).

`snmp_view_based_acm_mib:config_err/2` reaches OTP's one-slot frame and its instructions match after label normalization. `xref_utils:module_filename/2` reaches OTP's two-slot frame and matches the operand order, trim, and outer tail call. All 35 release xref observations agree with OTP. Eight exception stacks lose the extra caller frame present in the prior image. [SNMP instructions](snmp-config-err.txt) · [xref instructions](xref-module-filename.txt) · [xref probe](xref.txt).

The Windows OTP 29.1.1 comparison accepts all 1,366 sources and generates all 1,366 images. There are 385 Equal and 981 Different, with no status transitions. `ELDAPv3` remains Equal after C# ASN.1 generation. OTP loads both images in all 981 differing pairs. [Corpus](otp-corpus.json) · [Transitions](status-changes.txt) · [Loader](loader-and-code.txt).

Sixty-one images and 87 function bodies change. Frames move closer in 72 functions, including 59 that reach OTP's size. None move farther. Thirty-six functions gain normalized instruction equality, with no matches lost. The aggregate Code size gap falls by 973 bytes. [Frame delta](frame-delta.txt) · [Sizes](summary.txt) · [Function matches](normalized-functions.txt).

The terminal remote call group in the unused-entry-save audit falls from 101 functions in 44 modules to 76 functions in 23 modules. The audit overall falls from 1,011 functions in 392 modules to 975 functions in 380 modules. The terminal operator group still spans 43 modules with heterogeneous operators. The chain of tuple-producing calls has one shared register lifetime in 82 functions across 35 modules. [Entry saves](first-call-spills-after.txt) · [Tuple chain ranking](tuple-chains-after.txt) · [Nearest files](nearest.txt).

The final compiler block passes 3,721 of 3,829 tests, with the same 108 failures as Part 1508. The SDK formatter was applied to the new lambda layout after building the tested binaries. The full .NET suite was not rerun. Canonical readiness and publication remain pending. The readiness delta retains the immutable Part 561 baseline. [Compiler block](compiler-tests.json) · [Test status comparison](compiler-test-status.txt).
