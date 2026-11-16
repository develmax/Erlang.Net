# Part 1455 — Group atom and arity clauses

The C# BEAM emitter now groups clauses with an atom in the first argument and an integer arity in the second. Keys with identical arities and result share a branch. Branches that return the same atom share their return block. A singleton arity with an unconditional false fallback can return the exact-equality BIF result directly.

The fallback either reaches the function-clause error, runs atom and integer guards before returning false, returns false immediately, or tail-calls a local function with both original arguments. Pinned OTP 29.1.1 disassembly determines the branch order, shared return placement and opcode choice. The emitter uses the existing atom comparator, guard resolution, select operands and tail-call lowering.

An initial run used a headers-only source overlay. The seven source rejections were unresolved Markdown documentation directives. The corpus command below uses the pinned overlay that also supplies those documentation inputs.

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1455-tool-final
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1455-tool-final/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1455-checked.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
```

- [Pinned OTP instruction and execution comparison](probe.txt)
- [Focused .NET tests](focused.json) and [compiler assembly hash](compiler-hash.txt)

The complete input set admitted all 1,366 sources and generated all 1,366 images. There are 383 Equal and 983 Different results with zero status transitions. The C# ASN.1 generation and compilation of ELDAPv3 still produce the exact release image. Both loaders accepted all 983 differing images.

Three images changed. Ten erl_internal functions match OTP after rebasing labels. The bif/2 count falls from 923 instructions to 44 and is_type/2 from 241 to 21. The Code chunk moves from 8,145 to 3,770 bytes against 3,294 in OTP. The remaining functions, helper layout and metadata still differ.

The entry dispatch scan finds 1,547 functions in 379 images with at least two atom keys tested individually. Within that group, 394 functions in 24 images have OTP branch targets consisting of a tail call and optional literal moves. In megaco_text_parser_v1:yeccpars2_cont_4/7 the table selects x1 across 97 keys and forwards seven arguments to the next state. The fallback passes the original token to yeccerror/1.

- [Full corpus](otp-corpus.json), [status changes](status-changes.txt), and [loader and Code chunk checks](loader-and-code.txt)
- [Normalized instruction changes](normalized-functions.txt) and [erl_internal counts](erl-internal-functions.txt)
- [Entry dispatch ranking](entry-atom-dispatch.txt), [parser instructions](yecc-entry-dispatch.txt), and [remaining function differences](image-function-differences.txt)
