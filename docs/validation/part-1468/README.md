# Part 1468 — Infer list and integer operands from local callers

The register arithmetic emitter now consults the existing call fact analysis. For each arithmetic list entry, it starts from exports, escaped local functions and the module on-load function that can reach the entry. It follows local call arguments and results, including proper list construction and tail bindings. The emitted list test is omitted only when the merged caller facts prove a proper list. Integer operations provide integer results, and bit-list heads carry the interval 0 through 1. An unconstrained caller keeps the nil check. A call to a declared NIF has an unknown result because its native implementation can replace the Erlang body. This applies to the existing proper-list proof and the extended caller analysis.

In EnrollmentMessageSyntax-2009, the recursive make_and_set_list helper returns a proper bit list on success. encode_bitstring/1 and encode_bitstring/3 pass the list or its tails to unused_bitlist/3. The accumulator seeds are integers and the recursive arithmetic preserves that type. The resulting 18 instructions in unused_bitlist/3 match OTP after label normalization.

The control fixture covers a private function reached through a recursive list producer and a forwarding worker. It also covers a function reached from an exported entry with arbitrary list input. Both bodies match OTP instructions after removing the source path, and the tested results and improper-tail arguments agree. The whole fixture still differs in Code, Attr and Line because the producer and relay have other differences.

- [Permanent fixture](../../../tests/Erlang.Tests/Resources/Beam/beam_list_caller_facts.erl), [decoded instructions](probe.txt), [checked comparison](probe-checked.txt), and [fixture image comparison](probe-image.txt)
- [Native fallback probe](native-probe.txt) checks the nil test, the untyped list head and successful execution of the fallback.
- [Release instructions](unused-bitlist.txt) and [normalized comparison](unused-bitlist-normalized.txt)

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1468-tool-native
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1468-tool-native/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1468-native.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
```

The full Windows OTP 29.1.1 comparison generates 1,366 images with zero rejected sources, 383 Equal and 983 Different. ELDAPv3 remains Equal after C# ASN.1 generation. The Windows OTP loader accepts both images in all 983 differing pairs. Three focused .NET cases passed using the same compiler binary as the corpus.

Seven images change and all seven affected bodies gain normalized instruction equality. The encoded Code size gap increases by 33 bytes as typed operands expand the previously untyped instructions. All seven frame sizes stay the same. Across the full corpus, each differing image has four or more differing section payloads. Function labels and the default version attribute depend on code layout, so some section differences share a cause.

- [Full corpus](otp-corpus.json), [status changes](status-changes.txt), and [OTP loading](loader-and-code.txt)
- [Normalized functions](normalized-functions.txt), [frame and Code totals](summary.txt), and [section payload counts](chunk-payloads.txt)
- [Focused tests](focused.json) and [compiler hashes](compiler-hash.txt)
- [Frame ranking](frame-ranking.txt), [list-entry ranking](list-entry-ranking.txt), and [six release examples](list-entry-examples.txt)

The next group has 203 functions in 124 modules. Their clauses contain a tuple at the head of a list and a sequence in the body. Six release examples show that OTP delays allocation until after matching. Compare several of these functions when extending the shared entry lowering.

The full .NET suite and canonical readiness validation remain pending.
