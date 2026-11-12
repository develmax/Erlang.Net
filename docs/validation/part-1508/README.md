# Part 1508 — Preserve registers through terminal operators

The general emitter plans a terminal binary operator whose operands are one ordinary call and one literal or entry argument. It reuses entry call planning, including nested call arguments, and preserves the other argument only when the operator reads it after the call. Arithmetic consumes the result from X0. Floating division uses the existing floating register sequence. List append and subtraction use the tail call to the operator and retain operand order. [Compiler implementation](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.Subtraction.cs).

The reference is pinned OTP `beam_ssa_pre_codegen` and the release instructions for `xref_utils:application_filename/1`. The change reuses call planning, frame emission, and operand writing. No dependency was added.

The fixture covers both operand orders, append, list subtraction, arithmetic, floating division, and an ordinary call with a nested call argument. All six frame sizes reach the OTP size. OTP and both C# images agree on 414 returns, error reasons, and module frames in exception stacks. [Fixture](beam_terminal_operator_call.erl) · [Oracle](terminal-operator-probe.escript) · [Results](fixture.txt).

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1508-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1508-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1508-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1508-final.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
dotnet artifacts/beam-backend/part1508-tests/Erlang.Tests.dll --case compiler/beam/documentation-payload-matches-otp --case compiler/beam/rebuild-installed-otp-release-images --case compiler/beam/whole-module-matches-otp --case compiler/beam/call-result-bif-keeps-register --case compiler/beam/call-result-case-keeps-register --report artifacts/beam-backend/part1508-focused.json
dotnet artifacts/beam-backend/part1508-tests/Erlang.Tests.dll --block compiler --report artifacts/beam-backend/part1508-compiler.json
```

Both builds pass with no warnings or errors. Five focused tests pass. The corpus, test harness, and fixture tools use the same compiler binary. [Focused tests](focused.json) · [Compiler hashes](compiler-hash.txt).

`xref_utils:application_filename/1` moves from one stack slot to zero. Its instructions match the release after label normalization, including a single call-site line and the tail append. The nine release observations agree across OTP and both C# images. [Instructions](xref-application-filename.txt) · [Results](xref.txt).

The Windows OTP 29.1.1 comparison accepts all 1,366 sources and generates all 1,366 images. There are 385 Equal and 981 Different, with no status transitions. `ELDAPv3` remains Equal after C# ASN.1 generation. OTP loads both images in all 981 differing pairs. [Corpus](otp-corpus.json) · [Transitions](status-changes.txt) · [Loader](loader-and-code.txt).

Fourteen images and 16 function bodies change. Fifteen functions reach the OTP frame size and one already had the same size. Thirteen functions gain normalized instruction equality, with no previous matches lost. The aggregate Code size gap falls by 159 bytes. [Frame delta](frame-delta.txt) · [Sizes](summary.txt) · [Function matches](normalized-functions.txt).

The terminal operator group drops from 69 functions in 53 modules to 56 functions in 43 modules. The largest remaining group with source information is now a terminal remote call, with 101 functions in 44 modules. Its most common argument shape by module count is an append followed by a variable, with 18 functions in 16 modules. In `snmp_view_based_acm_mib:config_err/2`, OTP keeps the format in X until the append and saves one slot for the outer tail call. In `xref_utils:module_filename/2`, the terminal `filename:join/2` consumes a concatenation built from two calls. OTP uses two slots and trims before append. The current C# emitter uses three slots and emits an ordinary call to `filename:join/2`. [Entry saves](first-call-spills-after.txt) · [Operator shapes](operator-shapes-after.txt) · [Remote call shapes](remote-call-shapes.txt) · [SNMP instructions](snmp-config-err.txt) · [xref instructions](xref-module-filename.txt).

The compiler block passes 3,721 of 3,829 tests, with the same 108 failures as Part 1507. The SDK formatter was applied to the new code's layout after the compiler binaries were built. The full .NET suite was not rerun. Canonical readiness and publication remain pending. The readiness delta retains the immutable Part 561 baseline. [Compiler block](compiler-tests.json) · [Test status comparison](compiler-test-status.txt).
