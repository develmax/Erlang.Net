# Part 1116 — reuse last-use inputs across multiple sibling calls

The sibling call planner now accepts multiple ordinary calls in the arguments of a final tail call. It walks the calls backward to find the last use of each entry parameter and emits the calls in source order. It reuses a last-use slot with a swap or an ordered pair of moves, and uses dedicated slots when no input is available to reuse.

OTP 29.1.1 and the generated code match instruction for instruction for three siblings with successive swaps, the existing nonzero destination case, and four siblings with both dedicated slots and a last-use slot. The four-call case also verifies repeated and retained input values.

- [Three-sibling probe](beam_sibling_three_probe.erl)
- [Execution and instruction assertions](run.eval)
- [Matching disassembly](disassembly.txt)
- [Four-sibling probe](beam_sibling_four_probe.erl)
- [Four-sibling disassembly](four-disassembly.txt)
- [Four-sibling outcomes](four-outcomes.txt)

The full corpus generated 1,363 images. There were 348 Equal, 1,015 Different and three SourceRejected. ELDAPv3 remains Equal. All seven changed images have a smaller Code length gap. OTP accepted all differing images during loader preparation.

- [Focused compiler tests](focused.json)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader preparation](loader.txt)
