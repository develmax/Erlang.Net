# Part 1113 — reuse a saved input when evaluating sibling calls

The entry call planner handles two ordinary sibling calls whose arguments are entry variables or constants. It evaluates them in source order. When the first operand of the second call is read for the last time and occurs only once in that call, its Y slot is reused with swap. Otherwise the first result is preserved in a dedicated slot.

The OTP 29.1.1 instruction probe matches the complete instruction list of the swap case. An execution probe verified successful results, the order of side effects, propagation of an exception before the second call, and preserving an input required by the final call.

- [Instruction probe source](beam_nested_siblings_probe.erl)
- [Execution and instruction assertions](run.eval)
- [Matching disassembly](disassembly.txt)
- [Effects probe source](beam_sibling_effects_probe.erl)
- [OTP and generated execution outcomes](effects-outcomes.txt)

When the right input remains live, both compilers use two slots. OTP reserves Y0 for the first result and initializes it before saving the entry input in Y1. The generated code uses the opposite slot order.

- [Retained operand disassembly](retained-disassembly.txt)

The full corpus produced 1,363 images. There were 348 Equal, 1,015 Different and three SourceRejected. ELDAPv3 remains Equal. All 34 changed images have a smaller Code length gap. OTP accepted all differing images in loader preparation.

- [Focused compiler tests](focused.json)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader preparation](loader.txt)
