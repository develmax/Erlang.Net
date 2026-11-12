# Part 1114 — initialize the dedicated sibling result in Y0

When sibling calls cannot reuse the second call's input slot, the emitter now reserves Y0 for the first result and initializes it before copying the live entry operands. Both sibling probe functions match the OTP 29.1.1 instructions, and the execution checks cover retained inputs and left-to-right exception behavior.

The next probe shows that OTP reuses last-use input slots even when the input goes to an argument other than X0. It loads the input before storing the previous result in its slot. The three-call case shows consecutive swaps.

- [Probe source](beam_sibling_effects_probe.erl)
- [Execution and instruction assertions](run.eval)
- [Matching disassembly](disassembly.txt)
- [Three siblings and nonzero argument position](beam_sibling_three_probe.erl)
- [OTP instructions for the next cases](next-otp.txt)

The full corpus produced 1,363 images. There were 348 Equal, 1,015 Different and three SourceRejected. ELDAPv3 remains Equal. Five generated images changed without changing the Code length gap. The instruction comparison excluding line and label directives found one newly matching function, dialyzer_typesig:mk_constraint_any/1. OTP accepted all differing images in loader preparation.

- [Focused compiler tests](focused.json)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader preparation](loader.txt)
- [Function instruction comparison](function-prefix.txt)
