# Part 1472 — Forward a call result into case and infer local list results

When a leading call assigns a variable used only as the selector of the next case, the compiler feeds x0 directly into the existing case entry lowering. The reference check includes the case patterns, guards and bodies and the following expressions. It retains the binding when a branch or a later expression needs it.

The nested BIF planner uses the existing interprocedural result analysis for a local call passed to length. It annotates the operand only when the call is proven to return a proper list on success. Calls declared as native replacements still have unknown results.

The control case with a bound branch result drops from three frame slots to two. Its previously saved selector is read directly from x0. The guard example retains the binding and emits the same instructions as the earlier C# compiler. The OTP image and both C# versions produce the same tested values and case_clause error. The control image has other instruction and metadata differences.

- [Control source](../../../tests/Erlang.Tests/Resources/Beam/beam_entry_result_case.erl) and [comparison](probe.txt)

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1472-tool
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1472-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1472.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
```

The Windows OTP 29.1.1 comparison generates all 1,366 images. It records 383 Equal and 983 Different with zero rejected sources, compiler errors and status transitions. ELDAPv3 remains Equal after C# ASN.1 generation. OTP accepts both images in every differing pair. Five focused .NET cases pass using the same compiler binary as the corpus.

Seven images change. The eight public_key encoders gain normalized instruction equality. No functions lose it. Three functions in asn1ct, erts_internal and erts_literal_area_collector stop immediately spilling x0 to Y. The changed release frame sizes stay the same and the aggregate Code size gap falls by nine bytes. The additional typed operands grow the four public_key Code sections by four bytes each.

The current corpus has 229 first result reloads across 153 modules in the ranked frame group. The largest source shape is a case after the initial call in 52 functions across 40 modules. A separate source experiment puts the original binding into each branch pattern. It moves the tuple test before the save and keeps the three-slot frame. The next comparison needs register-aware guard operands and branch-local saves.

- [Full corpus](otp-corpus.json), [status changes](status-changes.txt), and [OTP loading](loader-and-code.txt)
- [Normalized functions](normalized-functions.txt), [result spills](spill-delta.txt), and [frame and Code totals](summary.txt)
- [Release prefixes](release-prefixes.txt), [alias experiment](alias-probe.txt), and [reload ranking](reload-ranking.txt)
- [Focused tests](focused.json) and [compiler hashes](compiler-hash.txt)

The full .NET suite was not rerun for this candidate. Canonical readiness and publication remain pending. The readiness comparison baseline remains the immutable Part 561 snapshot.
