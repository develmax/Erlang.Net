# Part 1467 — Schedule list entry arithmetic without a frame

The generator handles disjoint empty-list and cons clauses when one returns a tuple and the other tail-calls. A register plan evaluates arithmetic trees in Erlang evaluation order and tracks the remaining uses of each operand. It emits the nonempty-list test and list extraction before arithmetic. The nil path keeps the original argument for function_clause unless the existing local caller proof establishes a proper list.

Each GC BIF reports the live register range. A computed result can reuse a register once its previous value has no later use. Later operations reuse number and integer facts established by successful arithmetic. The plan preserves the tail argument moves and restores the source line before the empty branch.

The deterministic fixture drops from 36 instructions with a frame to 19 with none. Its entire 668-byte image matches OTP 29.1.1. The execution comparisons cover repeated arithmetic operands, the empty result, badarith and function_clause with the improper tail-call arguments.

- [Permanent Erlang fixture](../../../tests/Erlang.Tests/Resources/Beam/beam_arithmetic_list_entry.erl) and [OTP reference image](../../../tests/Erlang.Tests/Resources/Beam/beam_arithmetic_list_entry.beam)
- [Initial instruction comparison](probe.txt), [checked instructions and execution](probe-checked.txt), and [byte comparison](probe-image.txt)

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1467-tool-checked
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1467-tool-checked/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1467-checked.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
```

The full Windows OTP 29.1.1 corpus admits all 1,366 sources and generates 1,366 images. It records 383 Equal and 983 Different with zero status transitions. ELDAPv3 remains Equal after C# ASN.1 generation. OTP accepts both images of each differing pair. Three focused .NET cases passed, including the embedded byte comparison.

Seven release images change and their seven affected functions now have the OTP frame size. The aggregate Code size gap falls by 386 bytes. No release functions gain complete normalized instruction equality. In EnrollmentMessageSyntax-2009, unused_bitlist/3 drops from 37 instructions to 19, compared with 18 in OTP. The remaining differences are the nil check and type annotations derived by OTP from the local callers. The list-frame cohort has 2,856 functions in 530 modules, down from 2,863 functions.

- [Full corpus](otp-corpus.json), [status changes](status-changes.txt), and [OTP loading](loader-and-code.txt)
- [Release instructions](unused-bitlist.txt), [frame and Code totals](summary.txt), and [current frame ranking](frame-ranking.txt)
- [Focused tests](focused.json) and [compiler hashes](compiler-hash.txt)

The full .NET suite and canonical readiness validation remain pending.
